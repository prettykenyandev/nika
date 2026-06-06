using MediatR;
using Microsoft.EntityFrameworkCore;
using NikaFitness.Application.Catalog.Dtos;
using NikaFitness.Application.Common.Exceptions;
using NikaFitness.Application.Common.Interfaces;
using NikaFitness.Domain.ValueObjects;

namespace NikaFitness.Application.Catalog.Queries;

public sealed record GetProductBySlugQuery(string Slug) : IRequest<ProductDetailDto>;

public sealed class GetProductBySlugQueryHandler(IApplicationDbContext db)
    : IRequestHandler<GetProductBySlugQuery, ProductDetailDto>
{
    public async Task<ProductDetailDto> Handle(
        GetProductBySlugQuery request,
        CancellationToken cancellationToken)
    {
        var slug = Slug.FromExisting(request.Slug.Trim().ToLower());

        var product = await db.Products
            .AsNoTracking()
            .Include(p => p.Variants)
            .Where(p => p.IsPublished && p.Slug == slug)
            .FirstOrDefaultAsync(cancellationToken)
            ?? throw new NotFoundException("Product", request.Slug);

        var categoryName = await db.Categories
            .Where(c => c.Id == product.CategoryId)
            .Select(c => c.Name)
            .FirstOrDefaultAsync(cancellationToken);

        var variants = product.Variants
            .Select(v => new ProductVariantDto(
                v.Id,
                v.Sku,
                v.Name,
                v.Price.Amount,
                v.Price.Currency,
                v.StockQuantity,
                v.StockQuantity > 0))
            .ToList();

        return new ProductDetailDto(
            product.Id,
            product.Name,
            product.Slug.Value,
            product.Description,
            product.CategoryId,
            categoryName ?? string.Empty,
            product.ImageUrls.ToList(),
            variants);
    }
}
