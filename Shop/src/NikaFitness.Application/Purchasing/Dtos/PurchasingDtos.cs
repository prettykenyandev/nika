namespace NikaFitness.Application.Purchasing.Dtos;

public sealed record VendorSummaryDto(
    Guid Id,
    string Name,
    string? ContactName,
    string? Email,
    string? Phone,
    int PaymentTermDays,
    bool IsActive);

public sealed record VendorDetailDto(
    Guid Id,
    string Name,
    string? ContactName,
    string? Email,
    string? Phone,
    string? AddressLine1,
    string? City,
    string? Country,
    string? TaxIdentifier,
    int PaymentTermDays,
    string? Notes,
    bool IsActive);

public sealed record PurchaseOrderLineDto(
    Guid Id,
    Guid? ProductVariantId,
    string Description,
    string? Sku,
    decimal Quantity,
    decimal QuantityReceived,
    decimal QuantityOutstanding,
    decimal UnitCost,
    decimal TaxPercent,
    decimal LineNet,
    decimal LineTax,
    decimal LineTotal);

public sealed record PurchaseOrderSummaryDto(
    Guid Id,
    string PoNumber,
    Guid VendorId,
    string VendorName,
    DateOnly OrderDate,
    DateOnly? ExpectedDate,
    string Currency,
    string Status,
    decimal Total);

public sealed record PurchaseOrderDetailDto(
    Guid Id,
    string PoNumber,
    Guid VendorId,
    string VendorName,
    DateOnly OrderDate,
    DateOnly? ExpectedDate,
    string Currency,
    string Status,
    string? Notes,
    Guid? GeneratedBillId,
    decimal Subtotal,
    decimal TaxTotal,
    decimal Total,
    IReadOnlyList<PurchaseOrderLineDto> Lines);
