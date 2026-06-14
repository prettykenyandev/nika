using MediatR;
using Microsoft.EntityFrameworkCore;
using NikaFitness.Application.Common.Exceptions;
using NikaFitness.Application.Common.Interfaces;
using NikaFitness.Domain.Common;
using NikaFitness.Domain.Receivables;
using NikaFitness.Domain.Settings;
using NikaFitness.Domain.ValueObjects;

namespace NikaFitness.Application.Receivables.Commands;

/// <summary>
/// Generates a draft AR invoice from an existing customer order, copying its line items
/// at the prices captured on the order. Each order yields at most one invoice.
/// </summary>
public sealed record CreateInvoiceFromOrderCommand(
    Guid OrderId,
    int? PaymentTermDays) : IRequest<Guid>;

public sealed class CreateInvoiceFromOrderCommandHandler(
    IApplicationDbContext db,
    IDocumentNumberGenerator numbers)
    : IRequestHandler<CreateInvoiceFromOrderCommand, Guid>
{
    public async Task<Guid> Handle(
        CreateInvoiceFromOrderCommand request,
        CancellationToken cancellationToken)
    {
        var order = await db.Orders
            .AsNoTracking()
            .Include(o => o.Items)
            .FirstOrDefaultAsync(o => o.Id == request.OrderId, cancellationToken)
            ?? throw new NotFoundException("Order", request.OrderId);

        if (order.Items.Count == 0)
            throw new DomainException("Cannot invoice an order with no items.");

        var existing = await db.Invoices
            .AsNoTracking()
            .AnyAsync(i => i.OrderId == request.OrderId && i.Status != InvoiceStatus.Void,
                cancellationToken);
        if (existing)
            throw new DomainException("This order already has an invoice.");

        var settings = await db.CompanySettings.AsNoTracking()
            .FirstOrDefaultAsync(cancellationToken) ?? CompanySettings.CreateDefault();

        var issueDate = DateOnly.FromDateTime(DateTime.UtcNow);
        var termDays = request.PaymentTermDays is > 0 ? request.PaymentTermDays.Value : 14;
        var dueDate = issueDate.AddDays(termDays);

        var invoiceNumber = await numbers.NextAsync(
            "invoice", settings.InvoiceNumberPrefix, cancellationToken);

        var invoice = Invoice.Create(
            invoiceNumber,
            order.CustomerEmail,
            issueDate,
            dueDate,
            order.Currency,
            order.CustomerEmail,
            $"Generated from order {order.OrderNumber}.",
            order.CustomerId,
            order.Id);

        // Order prices are tax-inclusive retail prices, so the invoice carries no extra tax.
        foreach (var item in order.Items)
        {
            invoice.AddLine(
                $"{item.ProductName} ({item.Sku})",
                item.Quantity,
                new Money(item.UnitPrice.Amount, order.Currency),
                TaxRate.Zero);
        }

        db.Invoices.Add(invoice);
        await db.SaveChangesAsync(cancellationToken);
        return invoice.Id;
    }
}
