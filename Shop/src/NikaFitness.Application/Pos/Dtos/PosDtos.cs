namespace NikaFitness.Application.Pos.Dtos;

/// <summary>A single sellable variant resolved by SKU, for the point-of-sale screen.</summary>
public sealed record PosVariantDto(
    Guid VariantId,
    string Sku,
    string VariantName,
    Guid ProductId,
    string ProductName,
    decimal Price,
    string Currency,
    int StockQuantity,
    string? ImageUrl);

/// <summary>The outcome of a till sale paid by Card or M-Pesa.</summary>
public sealed record PosSaleResult(
    Guid OrderId,
    string OrderNumber,
    decimal Total,
    string Currency,
    string Method,
    string Status,
    string Message,
    DateTime? PaidAtUtc);
