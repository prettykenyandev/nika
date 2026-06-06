namespace NikaFitness.Application.Catalog.Dtos;

public sealed record CategoryDto(Guid Id, string Name, string Slug, string? Description);

public sealed record ProductVariantDto(
    Guid Id,
    string Sku,
    string Name,
    decimal Price,
    string Currency,
    int StockQuantity,
    bool InStock);

public sealed record ProductSummaryDto(
    Guid Id,
    string Name,
    string Slug,
    string CategoryName,
    decimal FromPrice,
    string Currency,
    string? PrimaryImageUrl,
    bool InStock);

public sealed record ProductDetailDto(
    Guid Id,
    string Name,
    string Slug,
    string Description,
    Guid CategoryId,
    string CategoryName,
    IReadOnlyList<string> ImageUrls,
    IReadOnlyList<ProductVariantDto> Variants);
