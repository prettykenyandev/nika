using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using NikaFitness.Application.Common.Interfaces;
using NikaFitness.Domain.Expenses;
using NikaFitness.Domain.Settings;
using NikaFitness.Domain.ValueObjects;

namespace NikaFitness.Application.Expenses.Commands;

/// <summary>A single line item supplied when creating or editing a bill.</summary>
public sealed record BillLineInput(
    string Description,
    Guid? ExpenseCategoryId,
    decimal Quantity,
    decimal UnitCost,
    decimal? TaxPercent);

public sealed record CreateBillCommand(
    string VendorName,
    DateOnly IssueDate,
    DateOnly DueDate,
    string? Currency,
    string? SupplierReference,
    string? Notes,
    string? AttachmentUrl,
    Guid? VendorId,
    IReadOnlyList<BillLineInput> Lines) : IRequest<Guid>;

public sealed class CreateBillCommandValidator : AbstractValidator<CreateBillCommand>
{
    public CreateBillCommandValidator()
    {
        RuleFor(x => x.VendorName).NotEmpty().MaximumLength(200);
        RuleFor(x => x.SupplierReference).MaximumLength(100);
        RuleFor(x => x.Notes).MaximumLength(1000);
        RuleFor(x => x.DueDate)
            .GreaterThanOrEqualTo(x => x.IssueDate)
            .WithMessage("Due date cannot be before the issue date.");
        RuleFor(x => x.Lines).NotEmpty().WithMessage("A bill needs at least one line.");
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

public sealed class CreateBillCommandHandler(
    IApplicationDbContext db,
    IDocumentNumberGenerator numbers)
    : IRequestHandler<CreateBillCommand, Guid>
{
    public async Task<Guid> Handle(CreateBillCommand request, CancellationToken cancellationToken)
    {
        var settings = await db.CompanySettings.AsNoTracking()
            .FirstOrDefaultAsync(cancellationToken) ?? CompanySettings.CreateDefault();

        var currency = string.IsNullOrWhiteSpace(request.Currency)
            ? settings.Currency
            : request.Currency;
        var defaultTax = settings.DefaultTaxRate;

        var billNumber = await numbers.NextAsync("bill", settings.BillNumberPrefix, cancellationToken);

        var bill = Bill.Create(
            billNumber,
            request.VendorName,
            request.IssueDate,
            request.DueDate,
            currency,
            request.SupplierReference,
            request.Notes,
            request.VendorId);

        foreach (var line in request.Lines)
        {
            var taxRate = line.TaxPercent.HasValue ? new TaxRate(line.TaxPercent.Value) : defaultTax;
            bill.AddLine(
                line.Description,
                line.ExpenseCategoryId,
                line.Quantity,
                new Money(line.UnitCost, currency),
                taxRate);
        }

        if (!string.IsNullOrWhiteSpace(request.AttachmentUrl))
            bill.SetAttachment(request.AttachmentUrl);

        db.Bills.Add(bill);
        await db.SaveChangesAsync(cancellationToken);
        return bill.Id;
    }
}
