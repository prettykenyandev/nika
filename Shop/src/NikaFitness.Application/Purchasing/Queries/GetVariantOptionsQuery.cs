using MediatR;
using Microsoft.EntityFrameworkCore;
using NikaFitness.Application.Common.Interfaces;

namespace NikaFitness.Application.Purchasing.Queries;

/// <summary>A product variant offered as a pick-list option when building a purchase order.</summary>
public sealed record VariantOptionDto(
    Guid VariantId,
    string Sku,
    string Label,
    int StockQuantity,
    decimal Price,
    string Currency);

public sealed record GetVariantOptionsQuery(string? Search = null) : IRequest<IReadOnlyList<VariantOptionDto>>;

public sealed class GetVariantOptionsQueryHandler(IApplicationDbContext db)
    : IRequestHandler<GetVariantOptionsQuery, IReadOnlyList<VariantOptionDto>>
{
    public async Task<IReadOnlyList<VariantOptionDto>> Handle(
        GetVariantOptionsQuery request,
        CancellationToken cancellationToken)
    {
        var products = await db.Products
            .AsNoTracking()
            .Include(p => p.Variants)
            .ToListAsync(cancellationToken);

        var options = products
            .SelectMany(p => p.Variants.Select(v => new VariantOptionDto(
                v.Id,
                v.Sku,
                $"{p.Name} — {v.Name} ({v.Sku})",
                v.StockQuantity,
                v.Price.Amount,
                v.Price.Currency)))
            .ToList();

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var term = request.Search.Trim().ToLower();
            options = options.Where(o =>
                o.Label.ToLower().Contains(term) || o.Sku.ToLower().Contains(term)).ToList();
        }

        return options
            .OrderBy(o => o.Label)
            .Take(500)
            .ToList();
    }
}
