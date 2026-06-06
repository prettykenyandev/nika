namespace NikaFitness.Application.Carts.Dtos;

public sealed record CartItemDto(
    Guid ProductId,
    Guid ProductVariantId,
    string ProductName,
    string VariantName,
    string Slug,
    string Sku,
    decimal UnitPrice,
    int Quantity,
    decimal LineTotal,
    string? ImageUrl,
    int AvailableStock);

public sealed record CartDto(
    string CartId,
    IReadOnlyList<CartItemDto> Items,
    decimal Subtotal,
    string Currency,
    int ItemCount);
