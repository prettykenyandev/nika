using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using NikaFitness.Application.Carts.Models;
using NikaFitness.Application.Common.Exceptions;
using NikaFitness.Application.Common.Interfaces;

namespace NikaFitness.Application.Carts.Commands;

public sealed record AddToCartCommand(string CartId, Guid ProductVariantId, int Quantity)
    : IRequest;

public sealed class AddToCartCommandValidator : AbstractValidator<AddToCartCommand>
{
    public AddToCartCommandValidator()
    {
        RuleFor(x => x.CartId).NotEmpty();
        RuleFor(x => x.ProductVariantId).NotEmpty();
        RuleFor(x => x.Quantity).GreaterThan(0).LessThanOrEqualTo(100);
    }
}

public sealed class AddToCartCommandHandler(IApplicationDbContext db, ICartStore cartStore)
    : IRequestHandler<AddToCartCommand>
{
    public async Task Handle(AddToCartCommand request, CancellationToken cancellationToken)
    {
        var variant = await db.Products
            .AsNoTracking()
            .Where(p => p.IsPublished && p.Variants.Any(v => v.Id == request.ProductVariantId))
            .SelectMany(p => p.Variants
                .Where(v => v.Id == request.ProductVariantId)
                .Select(v => new { ProductId = p.Id, VariantId = v.Id, v.StockQuantity }))
            .FirstOrDefaultAsync(cancellationToken)
            ?? throw new NotFoundException("Product variant", request.ProductVariantId);

        var cart = await cartStore.GetAsync(request.CartId, cancellationToken)
                   ?? new Cart { Id = request.CartId };

        var existingQty = cart.Lines
            .FirstOrDefault(l => l.ProductVariantId == request.ProductVariantId)?.Quantity ?? 0;

        if (existingQty + request.Quantity > variant.StockQuantity)
            throw new Common.Exceptions.ValidationException(
                new Dictionary<string, string[]>
                {
                    ["Quantity"] = [$"Only {variant.StockQuantity} item(s) in stock."]
                });

        cart.AddOrIncrement(variant.ProductId, variant.VariantId, request.Quantity);
        await cartStore.SaveAsync(cart, cancellationToken);
    }
}
