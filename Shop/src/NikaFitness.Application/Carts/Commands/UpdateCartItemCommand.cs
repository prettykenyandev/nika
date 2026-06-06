using FluentValidation;
using MediatR;
using NikaFitness.Application.Carts.Models;
using NikaFitness.Application.Common.Interfaces;

namespace NikaFitness.Application.Carts.Commands;

public sealed record UpdateCartItemCommand(string CartId, Guid ProductVariantId, int Quantity)
    : IRequest;

public sealed class UpdateCartItemCommandValidator : AbstractValidator<UpdateCartItemCommand>
{
    public UpdateCartItemCommandValidator()
    {
        RuleFor(x => x.CartId).NotEmpty();
        RuleFor(x => x.ProductVariantId).NotEmpty();
        RuleFor(x => x.Quantity).GreaterThanOrEqualTo(0).LessThanOrEqualTo(100);
    }
}

public sealed class UpdateCartItemCommandHandler(ICartStore cartStore)
    : IRequestHandler<UpdateCartItemCommand>
{
    public async Task Handle(UpdateCartItemCommand request, CancellationToken cancellationToken)
    {
        var cart = await cartStore.GetAsync(request.CartId, cancellationToken)
                   ?? new Cart { Id = request.CartId };

        cart.SetQuantity(request.ProductVariantId, request.Quantity);
        await cartStore.SaveAsync(cart, cancellationToken);
    }
}

public sealed record RemoveCartItemCommand(string CartId, Guid ProductVariantId) : IRequest;

public sealed class RemoveCartItemCommandHandler(ICartStore cartStore)
    : IRequestHandler<RemoveCartItemCommand>
{
    public async Task Handle(RemoveCartItemCommand request, CancellationToken cancellationToken)
    {
        var cart = await cartStore.GetAsync(request.CartId, cancellationToken);
        if (cart is null) return;

        cart.Remove(request.ProductVariantId);
        await cartStore.SaveAsync(cart, cancellationToken);
    }
}
