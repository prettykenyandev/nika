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

/// <summary>The outcome of a completed cash sale at the till.</summary>
public sealed record PosSaleResult(
    Guid OrderId,
    string OrderNumber,
    decimal Total,
    string Currency,
    decimal? AmountTendered,
    decimal? Change,
    DateTime PaidAtUtc);
