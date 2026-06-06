using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using NikaFitness.Application.Common.Exceptions;
using NikaFitness.Application.Common.Interfaces;
using NikaFitness.Application.Orders.Dtos;
using NikaFitness.Domain.Catalog;
using NikaFitness.Domain.Orders;
using NikaFitness.Domain.Payments;
using NikaFitness.Domain.ValueObjects;
using ValidationException = NikaFitness.Application.Common.Exceptions.ValidationException;

namespace NikaFitness.Application.Orders.Commands;

/// <summary>
/// Turns a cart into a placed order, reserves stock, and kicks off an M-Pesa STK push.
/// The order is created in <see cref="OrderStatus.PendingPayment"/>; the webhook later
/// confirms it.
/// </summary>
public sealed record CheckoutCommand(
    string CartId,
    string Email,
    string FullName,
    string Line1,
    string City,
    string Country,
    string PhoneNumber,
    string? Line2 = null,
    string? PostalCode = null) : IRequest<CheckoutResult>;

public sealed class CheckoutCommandValidator : AbstractValidator<CheckoutCommand>
{
    public CheckoutCommandValidator()
    {
        RuleFor(x => x.CartId).NotEmpty();
        RuleFor(x => x.Email).NotEmpty().EmailAddress();
        RuleFor(x => x.FullName).NotEmpty().MaximumLength(150);
        RuleFor(x => x.Line1).NotEmpty().MaximumLength(200);
        RuleFor(x => x.City).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Country).NotEmpty().MaximumLength(100);
        // M-Pesa expects an MSISDN such as 2547XXXXXXXX.
        RuleFor(x => x.PhoneNumber)
            .NotEmpty()
            .Matches(@"^(2547|2541)\d{8}$")
            .WithMessage("Phone must be in the format 2547XXXXXXXX.");
    }
}

public sealed class CheckoutCommandHandler(
    IApplicationDbContext db,
    ICartStore cartStore,
    ICurrentUser currentUser,
    IEnumerable<IPaymentGateway> gateways)
    : IRequestHandler<CheckoutCommand, CheckoutResult>
{
    public async Task<CheckoutResult> Handle(CheckoutCommand request, CancellationToken cancellationToken)
    {
        var cart = await cartStore.GetAsync(request.CartId, cancellationToken);
        if (cart is null || cart.Lines.Count == 0)
            throw new ValidationException(new Dictionary<string, string[]>
            {
                ["Cart"] = ["Your cart is empty."]
            });

        var variantIds = cart.Lines.Select(l => l.ProductVariantId).ToList();

        // Tracked load so we can reserve stock on the same entities we persist.
        var products = await db.Products
            .Where(p => p.Variants.Any(v => variantIds.Contains(v.Id)))
            .ToListAsync(cancellationToken);

        var address = new Address(
            request.FullName, request.Line1, request.City, request.Country,
            request.PhoneNumber, request.Line2, request.PostalCode);

        var order = Order.Create(request.Email, address, currentUser.UserId);

        // Map variant id -> (product, variant) for snapshotting and reservation.
        var lookup = products
            .SelectMany(p => p.Variants.Select(v => (Product: p, Variant: v)))
            .ToDictionary(x => x.Variant.Id);

        var reserved = new List<(ProductVariant Variant, int Quantity)>();

        foreach (var line in cart.Lines)
        {
            if (!lookup.TryGetValue(line.ProductVariantId, out var entry))
                throw new NotFoundException("Product variant", line.ProductVariantId);

            entry.Variant.Reserve(line.Quantity); // throws if insufficient stock
            reserved.Add((entry.Variant, line.Quantity));

            order.AddItem(
                entry.Product.Id,
                entry.Variant.Id,
                entry.Product.Name,
                entry.Variant.Sku,
                entry.Variant.Price,
                line.Quantity);
        }

        order.Place();

        var payment = Payment.ForOrder(order.Id, order.Total, PaymentMethod.Mpesa, request.PhoneNumber);

        db.Orders.Add(order);
        db.Payments.Add(payment);
        await db.SaveChangesAsync(cancellationToken);

        var gateway = gateways.FirstOrDefault(g => g.Method == PaymentMethod.Mpesa)
            ?? throw new InvalidOperationException("No M-Pesa payment gateway is configured.");

        var initiation = await gateway.InitiateAsync(
            new PaymentInitiationRequest(
                payment.Id,
                order.Id,
                order.OrderNumber,
                order.Total.Amount,
                order.Total.Currency,
                request.PhoneNumber),
            cancellationToken);

        if (initiation.Success && initiation.ProviderRequestId is not null)
        {
            payment.MarkProcessing(initiation.ProviderRequestId);
            await db.SaveChangesAsync(cancellationToken);
            await cartStore.RemoveAsync(request.CartId, cancellationToken);

            return new CheckoutResult(
                order.Id, order.OrderNumber, payment.Id, true,
                initiation.CustomerMessage ?? "Check your phone to authorise the M-Pesa payment.");
        }

        // Initiation failed: release the stock we reserved and fail the order.
        payment.MarkFailed(initiation.Error ?? "Payment could not be initiated.");
        order.MarkPaymentFailed(initiation.Error ?? "Payment could not be initiated.");
        foreach (var (variant, quantity) in reserved)
            variant.Restock(quantity);
        await db.SaveChangesAsync(cancellationToken);

        return new CheckoutResult(
            order.Id, order.OrderNumber, payment.Id, false,
            initiation.Error ?? "We couldn't start the payment. Please try again.");
    }
}
