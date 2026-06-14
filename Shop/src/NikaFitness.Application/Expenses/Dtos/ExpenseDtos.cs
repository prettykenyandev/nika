namespace NikaFitness.Application.Expenses.Dtos;

public sealed record ExpenseCategoryDto(
    Guid Id,
    string Name,
    string Slug,
    string? Description);

public sealed record BillLineDto(
    Guid Id,
    string Description,
    Guid? ExpenseCategoryId,
    string? ExpenseCategoryName,
    decimal Quantity,
    decimal UnitCost,
    decimal TaxPercent,
    decimal LineNet,
    decimal LineTax,
    decimal LineTotal);

public sealed record BillPaymentDto(
    Guid Id,
    decimal Amount,
    DateOnly PaidOn,
    string Method,
    string? Reference);

public sealed record BillSummaryDto(
    Guid Id,
    string BillNumber,
    string VendorName,
    DateOnly IssueDate,
    DateOnly DueDate,
    string Currency,
    string Status,
    bool IsOverdue,
    decimal Total,
    decimal AmountPaid,
    decimal AmountDue);

public sealed record BillDetailDto(
    Guid Id,
    string BillNumber,
    string VendorName,
    Guid? VendorId,
    string? SupplierReference,
    DateOnly IssueDate,
    DateOnly DueDate,
    string Currency,
    string Status,
    bool IsOverdue,
    string? Notes,
    string? AttachmentUrl,
    decimal Subtotal,
    decimal TaxTotal,
    decimal Total,
    decimal AmountPaid,
    decimal AmountDue,
    IReadOnlyList<BillLineDto> Lines,
    IReadOnlyList<BillPaymentDto> Payments);

/// <summary>Headline accounts-payable figures for dashboards.</summary>
public sealed record ApSummaryDto(
    string Currency,
    decimal Outstanding,
    decimal Overdue,
    int OpenBillCount,
    int OverdueBillCount);
