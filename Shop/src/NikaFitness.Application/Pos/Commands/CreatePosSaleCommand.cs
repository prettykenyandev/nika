using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using NikaFitness.Application.Common.Exceptions;
using NikaFitness.Application.Common.Interfaces;
using NikaFitness.Application.Pos.Dtos;
using NikaFitness.Domain.Catalog;
using NikaFitness.Domain.Orders;
using NikaFitness.Domain.Payments;
using NikaFitness.Domain.ValueObjects;
using ValidationException = NikaFitness.Application.Common.Exceptions.ValidationException;

namespace NikaFitness.Application.Pos.Commands;

public sealed record PosLineInput(Guid ProductVariantId, int Quantity);

/// <summary>
/// Rings up an in-store sale paid by <see cref="PaymentMethod.Card"/> or
/// <see cref="PaymentMethod.Mpesa"/> (cash is not accepted). Card settles immediately
/// (captured on an external terminal); M-Pesa places the order and triggers an STK push,
/// leaving it pending until the provider webhook confirms it.
/// </summary>
public sealed record CreatePosSaleCommand(
    IReadOnlyList<PosLineInput> Items,
    PaymentMethod Method,
    string? CustomerPhone = null,
    string? CustomerEmail = null) : IRequest<PosSaleResult>;

public sealed class CreatePosSaleCommandValidator : AbstractValidator<CreatePosSaleCommand>
{
    public CreatePosSaleCommandValidator()
    {
        RuleFor(x => x.Items)
            .NotEmpty().WithMessage("Add at least one item to the sale.");

        RuleForEach(x => x.Items).ChildRules(line =>
        {
            line.RuleFor(l => l.ProductVariantId).NotEmpty();
            line.RuleFor(l => l.Quantity).GreaterThan(0);
        });

        RuleFor(x => x.Method)
            .Must(m => m is PaymentMethod.Card or PaymentMethod.Mpesa)
            .WithMessage("The till only accepts Card or M-Pesa payments.");

        // M-Pesa needs the customer's MSISDN to send the STK push.
        RuleFor(x => x.CustomerPhone)
            .NotEmpty()
            .Matches(@"^(2547|2541)\d{8}$")
            .WithMessage("Enter the customer's M-Pesa phone as 2547XXXXXXXX.")
            .When(x => x.Method == PaymentMethod.Mpesa);
    }
}

public sealed class CreatePosSaleCommandHandler(
    IApplicationDbContext db,
    IEnumerable<IPaymentGateway> gateways)
    : IRequestHandler<CreatePosSaleCommand, PosSaleResult>
{
    private const string WalkInEmail = "walkin@pos.nikafitness.local";

    public async Task<PosSaleResult> Handle(CreatePosSaleCommand request, CancellationToken cancellationToken)
    {
        // Merge repeated scans of the same variant into a single line.
        var lines = request.Items
            .GroupBy(i => i.ProductVariantId)
            .Select(g => (VariantId: g.Key, Quantity: g.Sum(x => x.Quantity)))
            .ToList();

        var variantIds = lines.Select(l => l.VariantId).ToList();

        // Tracked load (with variants) so the stock we reserve is persisted.
        var products = await db.Products
            .Include(p => p.Variants)
            .Where(p => p.Variants.Any(v => variantIds.Contains(v.Id)))
            .ToListAsync(cancellationToken);

        var lookup = products
            .SelectMany(p => p.Variants.Select(v => (Product: p, Variant: v)))
            .ToDictionary(x => x.Variant.Id);

        var email = string.IsNullOrWhiteSpace(request.CustomerEmail)
            ? WalkInEmail
            : request.CustomerEmail!.Trim();

        // POS sales have no shipping; use a fixed in-store address placeholder.
        var address = new Address("Walk-in customer", "In-store POS", "Nairobi", "Kenya", "POS");
        var order = Order.Create(email, address);

        var reserved = new List<(ProductVariant Variant, int Quantity)>();

        foreach (var line in lines)
        {
            if (!lookup.TryGetValue(line.VariantId, out var entry))
                throw new NotFoundException("Product variant", line.VariantId);

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
        var total = order.Total;

        return request.Method == PaymentMethod.Card
            ? await SettleCardAsync(order, total, cancellationToken)
            : await InitiateMpesaAsync(order, total, request.CustomerPhone!, reserved, cancellationToken);
    }

    /// <summary>Card was taken on an external terminal — capture and settle immediately.</summary>
    private async Task<PosSaleResult> SettleCardAsync(Order order, Money total, CancellationToken ct)
    {
        var payment = Payment.ForOrder(order.Id, total, PaymentMethod.Card, "CARD");
        payment.MarkSucceeded($"POS-CARD-{order.OrderNumber}");
        order.MarkPaid();

        db.Orders.Add(order);
        db.Payments.Add(payment);
        await db.SaveChangesAsync(ct);

        return new PosSaleResult(
            order.Id, order.OrderNumber, total.Amount, total.Currency,
            "Card", "Paid", "Card payment captured.", order.PaidAtUtc ?? DateTime.UtcNow);
    }

    /// <summary>Send an M-Pesa STK push and leave the order pending until the webhook confirms.</summary>
    private async Task<PosSaleResult> InitiateMpesaAsync(
        Order order,
        Money total,
        string phone,
        List<(ProductVariant Variant, int Quantity)> reserved,
        CancellationToken ct)
    {
        var payment = Payment.ForOrder(order.Id, total, PaymentMethod.Mpesa, phone);

        db.Orders.Add(order);
        db.Payments.Add(payment);
        await db.SaveChangesAsync(ct);

        var gateway = gateways.FirstOrDefault(g => g.Method == PaymentMethod.Mpesa)
            ?? throw new InvalidOperationException("No M-Pesa payment gateway is configured.");

        var initiation = await gateway.InitiateAsync(
            new PaymentInitiationRequest(
                payment.Id, order.Id, order.OrderNumber,
                total.Amount, total.Currency, phone),
            ct);

        if (initiation.Success && initiation.ProviderRequestId is not null)
        {
            payment.MarkProcessing(initiation.ProviderRequestId);
            await db.SaveChangesAsync(ct);

            return new PosSaleResult(
                order.Id, order.OrderNumber, total.Amount, total.Currency,
                "M-Pesa", "Pending",
                initiation.CustomerMessage ?? "STK push sent — ask the customer to authorise on their phone.",
                null);
        }

        // Initiation failed: release the stock we reserved and fail the order.
        payment.MarkFailed(initiation.Error ?? "Payment could not be initiated.");
        order.MarkPaymentFailed(initiation.Error ?? "Payment could not be initiated.");
        foreach (var (variant, quantity) in reserved)
            variant.Restock(quantity);
        await db.SaveChangesAsync(ct);

        throw new ValidationException(new Dictionary<string, string[]>
        {
            ["Mpesa"] = [initiation.Error ?? "Could not start the M-Pesa payment. Please try again."]
        });
    }
}
