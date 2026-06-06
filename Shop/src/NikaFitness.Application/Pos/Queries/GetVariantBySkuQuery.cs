using MediatR;
using Microsoft.EntityFrameworkCore;
using NikaFitness.Application.Common.Interfaces;
using NikaFitness.Application.Pos.Dtos;

namespace NikaFitness.Application.Pos.Queries;

/// <summary>
/// Resolves a single product variant by its SKU (e.g. typed or barcode-scanned at the
/// till). Returns <c>null</c> when no variant matches. Includes unpublished products so
/// staff can ring up anything in inventory.
/// </summary>
public sealed record GetVariantBySkuQuery(string Sku) : IRequest<PosVariantDto?>;

public sealed class GetVariantBySkuQueryHandler(IApplicationDbContext db)
    : IRequestHandler<GetVariantBySkuQuery, PosVariantDto?>
{
    public async Task<PosVariantDto?> Handle(GetVariantBySkuQuery request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Sku))
            return null;

        var sku = request.Sku.Trim().ToUpperInvariant();

        var row = await db.Products
            .AsNoTracking()
            .Where(p => p.Variants.Any(v => v.Sku == sku))
            .Select(p => new
            {
                ProductId = p.Id,
                ProductName = p.Name,
                Images = EF.Property<List<string>>(p, "_imageUrls"),
                Variant = p.Variants
                    .Where(v => v.Sku == sku)
                    .Select(v => new
                    {
                        v.Id,
                        v.Sku,
                        v.Name,
                        v.Price.Amount,
                        v.Price.Currency,
                        v.StockQuantity
                    })
                    .FirstOrDefault()
            })
            .FirstOrDefaultAsync(cancellationToken);

        if (row?.Variant is null)
            return null;

        return new PosVariantDto(
            row.Variant.Id,
            row.Variant.Sku,
            row.Variant.Name,
            row.ProductId,
            row.ProductName,
            row.Variant.Amount,
            row.Variant.Currency,
            row.Variant.StockQuantity,
            row.Images.FirstOrDefault());
    }
}
