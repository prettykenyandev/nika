using MediatR;
using Microsoft.EntityFrameworkCore;
using NikaFitness.Application.Carts.Dtos;
using NikaFitness.Application.Carts.Models;
using NikaFitness.Application.Common.Interfaces;
using NikaFitness.Domain.ValueObjects;

namespace NikaFitness.Application.Carts.Queries;

public sealed record GetCartQuery(string CartId) : IRequest<CartDto>;

public sealed class GetCartQueryHandler(IApplicationDbContext db, ICartStore cartStore)
    : IRequestHandler<GetCartQuery, CartDto>
{
    public async Task<CartDto> Handle(GetCartQuery request, CancellationToken cancellationToken)
    {
        var cart = await cartStore.GetAsync(request.CartId, cancellationToken)
                   ?? new Cart { Id = request.CartId };

        if (cart.Lines.Count == 0)
            return new CartDto(request.CartId, [], 0m, Money.DefaultCurrency, 0);

        var variantIds = cart.Lines.Select(l => l.ProductVariantId).ToList();

        // Load every referenced variant (with its parent product) in a single round trip.
        var variantData = await db.Products
            .AsNoTracking()
            .Where(p => p.Variants.Any(v => variantIds.Contains(v.Id)))
            .SelectMany(p => p.Variants
                .Where(v => variantIds.Contains(v.Id))
                .Select(v => new
                {
                    ProductId = p.Id,
                    ProductName = p.Name,
                    Slug = p.Slug.Value,
                    Images = EF.Property<List<string>>(p, "_imageUrls"),
                    VariantId = v.Id,
                    VariantName = v.Name,
                    v.Sku,
                    Price = v.Price.Amount,
                    v.Price.Currency,
                    v.StockQuantity
                }))
            .ToListAsync(cancellationToken);

        var byVariant = variantData.ToDictionary(v => v.VariantId);

        var items = new List<CartItemDto>(cart.Lines.Count);
        var currency = Money.DefaultCurrency;

        foreach (var line in cart.Lines)
        {
            if (!byVariant.TryGetValue(line.ProductVariantId, out var v))
                continue; // variant was removed from the catalogue; skip silently

            currency = v.Currency;
            items.Add(new CartItemDto(
                v.ProductId,
                v.VariantId,
                v.ProductName,
                v.VariantName,
                v.Slug,
                v.Sku,
                v.Price,
                line.Quantity,
                v.Price * line.Quantity,
                v.Images.FirstOrDefault(),
                v.StockQuantity));
        }

        var subtotal = items.Sum(i => i.LineTotal);
        var itemCount = items.Sum(i => i.Quantity);

        return new CartDto(request.CartId, items, subtotal, currency, itemCount);
    }
}
