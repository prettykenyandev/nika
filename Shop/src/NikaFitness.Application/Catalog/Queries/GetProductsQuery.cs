using MediatR;
using Microsoft.EntityFrameworkCore;
using NikaFitness.Application.Catalog.Dtos;
using NikaFitness.Application.Common.Interfaces;
using NikaFitness.Application.Common.Models;
using NikaFitness.Domain.ValueObjects;

namespace NikaFitness.Application.Catalog.Queries;

public sealed record GetProductsQuery(
    string? CategorySlug = null,
    string? Search = null,
    int Page = 1,
    int PageSize = 12) : IRequest<PagedResult<ProductSummaryDto>>;

public sealed class GetProductsQueryHandler(IApplicationDbContext db)
    : IRequestHandler<GetProductsQuery, PagedResult<ProductSummaryDto>>
{
    public async Task<PagedResult<ProductSummaryDto>> Handle(
        GetProductsQuery request,
        CancellationToken cancellationToken)
    {
        var page = Math.Max(1, request.Page);
        var pageSize = Math.Clamp(request.PageSize, 1, 60);

        var query = db.Products
            .AsNoTracking()
            .Where(p => p.IsPublished);

        if (!string.IsNullOrWhiteSpace(request.CategorySlug))
        {
            var slug = Slug.FromExisting(request.CategorySlug.Trim().ToLower());
            query = query.Where(p => db.Categories
                .Any(c => c.Id == p.CategoryId && c.Slug == slug));
        }

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var term = request.Search.Trim().ToLower();
            query = query.Where(p => p.Name.ToLower().Contains(term));
        }

        var totalCount = await query.CountAsync(cancellationToken);

        var rows = await query
            .OrderByDescending(p => p.CreatedAtUtc)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(p => new
            {
                p.Id,
                p.Name,
                Slug = p.Slug.Value,
                CategoryName = db.Categories
                    .Where(c => c.Id == p.CategoryId)
                    .Select(c => c.Name)
                    .FirstOrDefault(),
                FromPrice = p.Variants.Min(v => v.Price.Amount),
                Currency = p.Variants.Select(v => v.Price.Currency).FirstOrDefault(),
                Images = EF.Property<List<string>>(p, "_imageUrls"),
                InStock = p.Variants.Any(v => v.StockQuantity > 0)
            })
            .ToListAsync(cancellationToken);

        var items = rows
            .Select(r => new ProductSummaryDto(
                r.Id,
                r.Name,
                r.Slug,
                r.CategoryName ?? string.Empty,
                r.FromPrice,
                r.Currency ?? Money.DefaultCurrency,
                r.Images.FirstOrDefault(),
                r.InStock))
            .ToList();

        return new PagedResult<ProductSummaryDto>(items, page, pageSize, totalCount);
    }
}
