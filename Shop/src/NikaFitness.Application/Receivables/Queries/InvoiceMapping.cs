using NikaFitness.Application.Receivables.Dtos;
using NikaFitness.Domain.Receivables;

namespace NikaFitness.Application.Receivables.Queries;

/// <summary>Maps invoice aggregates to their read DTOs (computed totals live on the domain).</summary>
internal static class InvoiceMapping
{
    public static InvoiceSummaryDto ToSummary(Invoice invoice) => new(
        invoice.Id,
        invoice.InvoiceNumber,
        invoice.CustomerName,
        invoice.IssueDate,
        invoice.DueDate,
        invoice.Currency,
        invoice.Status.ToString(),
        invoice.IsOverdue,
        invoice.Total.Amount,
        invoice.AmountPaid.Amount,
        invoice.AmountDue.Amount);

    public static InvoiceDetailDto ToDetail(Invoice invoice)
    {
        var lines = invoice.Lines.Select(l => new InvoiceLineDto(
            l.Id,
            l.Description,
            l.Quantity,
            l.UnitPrice.Amount,
            l.TaxRate.Percent,
            l.LineNet.Amount,
            l.LineTax.Amount,
            l.LineTotal.Amount)).ToList();

        var payments = invoice.Payments
            .OrderByDescending(p => p.ReceivedOn)
            .Select(p => new InvoicePaymentDto(p.Id, p.Amount.Amount, p.ReceivedOn, p.Method, p.Reference))
            .ToList();

        return new InvoiceDetailDto(
            invoice.Id,
            invoice.InvoiceNumber,
            invoice.CustomerName,
            invoice.CustomerId,
            invoice.CustomerEmail,
            invoice.OrderId,
            invoice.IssueDate,
            invoice.DueDate,
            invoice.Currency,
            invoice.Status.ToString(),
            invoice.IsOverdue,
            invoice.Notes,
            invoice.Subtotal.Amount,
            invoice.TaxTotal.Amount,
            invoice.Total.Amount,
            invoice.AmountPaid.Amount,
            invoice.AmountDue.Amount,
            lines,
            payments);
    }
}
