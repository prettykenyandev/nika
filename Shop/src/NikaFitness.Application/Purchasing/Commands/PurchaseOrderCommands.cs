using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using NikaFitness.Application.Common.Exceptions;
using NikaFitness.Application.Common.Interfaces;
using NikaFitness.Domain.Common;
using NikaFitness.Domain.Expenses;
using NikaFitness.Domain.Purchasing;
using NikaFitness.Domain.Settings;
using NikaFitness.Domain.ValueObjects;

namespace NikaFitness.Application.Purchasing.Commands;

public sealed record PurchaseOrderLineInput(
    Guid? ProductVariantId,
    string Description,
    string? Sku,
    decimal Quantity,
    decimal UnitCost,
    decimal? TaxPercent);

public sealed record CreatePurchaseOrderCommand(
    Guid VendorId,
    DateOnly OrderDate,
    DateOnly? ExpectedDate,
    string? Currency,
    string? Notes,
    IReadOnlyList<PurchaseOrderLineInput> Lines) : IRequest<Guid>;

public sealed class CreatePurchaseOrderCommandValidator : AbstractValidator<CreatePurchaseOrderCommand>
{
    public CreatePurchaseOrderCommandValidator()
    {
        RuleFor(x => x.VendorId).NotEmpty();
        RuleFor(x => x.Notes).MaximumLength(1000);
        RuleFor(x => x.Lines).NotEmpty().WithMessage("A purchase order needs at least one line.");
        RuleForEach(x => x.Lines).ChildRules(line =>
        {
            line.RuleFor(l => l.Description).NotEmpty().MaximumLength(300);
            line.RuleFor(l => l.Quantity).GreaterThan(0);
            line.RuleFor(l => l.UnitCost).GreaterThanOrEqualTo(0);
            line.RuleFor(l => l.TaxPercent).InclusiveBetween(0, 100)
                .When(l => l.TaxPercent.HasValue);
        });
    }
}

public sealed class CreatePurchaseOrderCommandHandler(
    IApplicationDbContext db,
    IDocumentNumberGenerator numbers)
    : IRequestHandler<CreatePurchaseOrderCommand, Guid>
{
    public async Task<Guid> Handle(CreatePurchaseOrderCommand request, CancellationToken cancellationToken)
    {
        var vendor = await db.Vendors.AsNoTracking()
            .FirstOrDefaultAsync(v => v.Id == request.VendorId, cancellationToken)
            ?? throw new NotFoundException("Vendor", request.VendorId);

        var settings = await db.CompanySettings.AsNoTracking()
            .FirstOrDefaultAsync(cancellationToken) ?? CompanySettings.CreateDefault();

        var currency = string.IsNullOrWhiteSpace(request.Currency) ? settings.Currency : request.Currency;
        var defaultTax = settings.DefaultTaxRate;

        var poNumber = await numbers.NextAsync(
            "purchase-order", settings.PurchaseOrderNumberPrefix, cancellationToken);

        var po = PurchaseOrder.Create(
            poNumber, vendor.Id, vendor.Name, request.OrderDate,
            request.ExpectedDate, currency, request.Notes);

        foreach (var line in request.Lines)
        {
            var taxRate = line.TaxPercent.HasValue ? new TaxRate(line.TaxPercent.Value) : defaultTax;
            po.AddLine(
                line.ProductVariantId,
                line.Description,
                line.Sku,
                line.Quantity,
                new Money(line.UnitCost, currency),
                taxRate);
        }

        db.PurchaseOrders.Add(po);
        await db.SaveChangesAsync(cancellationToken);
        return po.Id;
    }
}

public sealed record SendPurchaseOrderCommand(Guid Id) : IRequest;

public sealed class SendPurchaseOrderCommandHandler(IApplicationDbContext db)
    : IRequestHandler<SendPurchaseOrderCommand>
{
    public async Task Handle(SendPurchaseOrderCommand request, CancellationToken cancellationToken)
    {
        var po = await db.PurchaseOrders
            .Include(p => p.Lines)
            .FirstOrDefaultAsync(p => p.Id == request.Id, cancellationToken)
            ?? throw new NotFoundException("PurchaseOrder", request.Id);

        po.Send();
        await db.SaveChangesAsync(cancellationToken);
    }
}

public sealed record ReceiptItemInput(Guid LineId, decimal Quantity);

public sealed record ReceivePurchaseOrderCommand(
    Guid Id,
    IReadOnlyList<ReceiptItemInput> Receipts) : IRequest;

public sealed class ReceivePurchaseOrderCommandValidator : AbstractValidator<ReceivePurchaseOrderCommand>
{
    public ReceivePurchaseOrderCommandValidator()
    {
        RuleFor(x => x.Receipts).NotEmpty().WithMessage("Specify at least one line to receive.");
        RuleForEach(x => x.Receipts).ChildRules(r =>
            r.RuleFor(i => i.Quantity).GreaterThan(0));
    }
}

public sealed class ReceivePurchaseOrderCommandHandler(IApplicationDbContext db)
    : IRequestHandler<ReceivePurchaseOrderCommand>
{
    public async Task Handle(ReceivePurchaseOrderCommand request, CancellationToken cancellationToken)
    {
        var po = await db.PurchaseOrders
            .Include(p => p.Lines)
            .FirstOrDefaultAsync(p => p.Id == request.Id, cancellationToken)
            ?? throw new NotFoundException("PurchaseOrder", request.Id);

        var receipts = request.Receipts
            .Select(r => new ReceiptItem(r.LineId, r.Quantity))
            .ToList();

        var restocks = po.Receive(receipts);

        foreach (var restock in restocks)
        {
            var product = await db.Products
                .Include(p => p.Variants)
                .FirstOrDefaultAsync(p => p.Variants.Any(v => v.Id == restock.ProductVariantId), cancellationToken);

            product?.GetVariant(restock.ProductVariantId).Restock(restock.Quantity);
        }

        await db.SaveChangesAsync(cancellationToken);
    }
}

public sealed record ClosePurchaseOrderCommand(Guid Id, bool GenerateBill) : IRequest<Guid?>;

public sealed class ClosePurchaseOrderCommandHandler(
    IApplicationDbContext db,
    IDocumentNumberGenerator numbers)
    : IRequestHandler<ClosePurchaseOrderCommand, Guid?>
{
    public async Task<Guid?> Handle(ClosePurchaseOrderCommand request, CancellationToken cancellationToken)
    {
        var po = await db.PurchaseOrders
            .Include(p => p.Lines)
            .FirstOrDefaultAsync(p => p.Id == request.Id, cancellationToken)
            ?? throw new NotFoundException("PurchaseOrder", request.Id);

        Guid? billId = null;

        if (request.GenerateBill && po.GeneratedBillId is null)
        {
            var settings = await db.CompanySettings.AsNoTracking()
                .FirstOrDefaultAsync(cancellationToken) ?? CompanySettings.CreateDefault();
            var vendor = await db.Vendors.AsNoTracking()
                .FirstOrDefaultAsync(v => v.Id == po.VendorId, cancellationToken);

            var billLines = po.Lines
                .Where(l => l.QuantityReceived > 0)
                .ToList();

            if (billLines.Count == 0)
                throw new DomainException("Cannot generate a bill before any goods are received.");

            var issueDate = DateOnly.FromDateTime(DateTime.UtcNow);
            var termDays = vendor?.PaymentTermDays ?? 30;
            var dueDate = issueDate.AddDays(termDays);

            var billNumber = await numbers.NextAsync("bill", settings.BillNumberPrefix, cancellationToken);

            var bill = Bill.Create(
                billNumber,
                po.VendorName,
                issueDate,
                dueDate,
                po.Currency,
                po.PoNumber,
                $"Auto-generated from purchase order {po.PoNumber}.",
                po.VendorId);

            foreach (var line in billLines)
            {
                bill.AddLine(
                    line.Description,
                    null,
                    line.QuantityReceived,
                    new Money(line.UnitCost.Amount, po.Currency),
                    line.TaxRate);
            }

            db.Bills.Add(bill);
            po.RecordGeneratedBill(bill.Id);
            billId = bill.Id;
        }

        po.Close();
        await db.SaveChangesAsync(cancellationToken);
        return billId;
    }
}

public sealed record CancelPurchaseOrderCommand(Guid Id) : IRequest;

public sealed class CancelPurchaseOrderCommandHandler(IApplicationDbContext db)
    : IRequestHandler<CancelPurchaseOrderCommand>
{
    public async Task Handle(CancelPurchaseOrderCommand request, CancellationToken cancellationToken)
    {
        var po = await db.PurchaseOrders
            .Include(p => p.Lines)
            .FirstOrDefaultAsync(p => p.Id == request.Id, cancellationToken)
            ?? throw new NotFoundException("PurchaseOrder", request.Id);

        po.Cancel();
        await db.SaveChangesAsync(cancellationToken);
    }
}
