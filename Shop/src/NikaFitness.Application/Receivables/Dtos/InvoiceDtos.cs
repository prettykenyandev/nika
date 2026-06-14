namespace NikaFitness.Application.Receivables.Dtos;

public sealed record InvoiceLineDto(
    Guid Id,
    string Description,
    decimal Quantity,
    decimal UnitPrice,
    decimal TaxPercent,
    decimal LineNet,
    decimal LineTax,
    decimal LineTotal);

public sealed record InvoicePaymentDto(
    Guid Id,
    decimal Amount,
    DateOnly ReceivedOn,
    string Method,
    string? Reference);

public sealed record InvoiceSummaryDto(
    Guid Id,
    string InvoiceNumber,
    string CustomerName,
    DateOnly IssueDate,
    DateOnly DueDate,
    string Currency,
    string Status,
    bool IsOverdue,
    decimal Total,
    decimal AmountPaid,
    decimal AmountDue);

public sealed record InvoiceDetailDto(
    Guid Id,
    string InvoiceNumber,
    string CustomerName,
    Guid? CustomerId,
    string? CustomerEmail,
    Guid? OrderId,
    DateOnly IssueDate,
    DateOnly DueDate,
    string Currency,
    string Status,
    bool IsOverdue,
    string? Notes,
    decimal Subtotal,
    decimal TaxTotal,
    decimal Total,
    decimal AmountPaid,
    decimal AmountDue,
    IReadOnlyList<InvoiceLineDto> Lines,
    IReadOnlyList<InvoicePaymentDto> Payments);

/// <summary>Headline accounts-receivable figures for dashboards.</summary>
public sealed record ArSummaryDto(
    string Currency,
    decimal Outstanding,
    decimal Overdue,
    int OpenInvoiceCount,
    int OverdueInvoiceCount);
