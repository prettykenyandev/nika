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
/// Rings up an in-store cash sale: reserves stock, creates a <see cref="Order"/>, and
/// settles a <see cref="PaymentMethod.Cash"/> payment immediately (no async provider),
/// marking the order paid. Mirrors the storefront checkout but skips the M-Pesa round-trip.
/// </summary>
public sealed record CreatePosSaleCommand(
    IReadOnlyList<PosLineInput> Items,
    decimal? CashTendered = null,
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

        RuleFor(x => x.CashTendered)
            .GreaterThan(0).When(x => x.CashTendered.HasValue)
            .WithMessage("Cash tendered must be greater than zero.");
    }
}

public sealed class CreatePosSaleCommandHandler(IApplicationDbContext db)
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

        foreach (var line in lines)
        {
            if (!lookup.TryGetValue(line.VariantId, out var entry))
                throw new NotFoundException("Product variant", line.VariantId);

            entry.Variant.Reserve(line.Quantity); // throws if insufficient stock

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

        // If cash was entered it must cover the total.
        if (request.CashTendered is { } tendered && tendered < total.Amount)
        {
            throw new ValidationException(new Dictionary<string, string[]>
            {
                ["CashTendered"] =
                    [$"Cash tendered ({tendered:N2}) is less than the total ({total.Amount:N2})."]
            });
        }

        var payment = Payment.ForOrder(order.Id, total, PaymentMethod.Cash, "CASH");
        payment.MarkSucceeded(order.OrderNumber);
        order.MarkPaid();

        db.Orders.Add(order);
        db.Payments.Add(payment);
        await db.SaveChangesAsync(cancellationToken);

        decimal? change = request.CashTendered is { } t
            ? decimal.Round(t - total.Amount, 2, MidpointRounding.AwayFromZero)
            : null;

        return new PosSaleResult(
            order.Id,
            order.OrderNumber,
            total.Amount,
            total.Currency,
            request.CashTendered,
            change,
            order.PaidAtUtc ?? DateTime.UtcNow);
    }
}
