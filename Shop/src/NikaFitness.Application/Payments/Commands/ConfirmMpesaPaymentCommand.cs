using MediatR;
using Microsoft.EntityFrameworkCore;
using NikaFitness.Application.Common.Interfaces;
using NikaFitness.Domain.Payments;

namespace NikaFitness.Application.Payments.Commands;

/// <summary>
/// Applies the result of an M-Pesa STK push, as delivered to our webhook. Correlated
/// to a payment by the provider request id (CheckoutRequestID). Idempotent: replays
/// from Safaricom are safe.
/// </summary>
public sealed record ConfirmMpesaPaymentCommand(
    string ProviderRequestId,
    bool Success,
    string? ProviderReference,
    string? ResultDescription) : IRequest;

public sealed class ConfirmMpesaPaymentCommandHandler(IApplicationDbContext db)
    : IRequestHandler<ConfirmMpesaPaymentCommand>
{
    public async Task Handle(ConfirmMpesaPaymentCommand request, CancellationToken cancellationToken)
    {
        var payment = await db.Payments
            .FirstOrDefaultAsync(p => p.ProviderRequestId == request.ProviderRequestId, cancellationToken);

        // Unknown request id — nothing to do (could be a stale/duplicate callback).
        if (payment is null)
            return;

        // Already in a terminal state — idempotent no-op.
        if (payment.Status is PaymentStatus.Succeeded or PaymentStatus.Failed)
            return;

        var order = await db.Orders
            .FirstOrDefaultAsync(o => o.Id == payment.OrderId, cancellationToken);

        if (request.Success)
        {
            payment.MarkSucceeded(request.ProviderReference ?? "UNKNOWN");
            order?.MarkPaid();
        }
        else
        {
            var reason = request.ResultDescription ?? "Payment was not completed.";
            payment.MarkFailed(reason);
            order?.MarkPaymentFailed(reason);
            await RestockAsync(order, cancellationToken);
        }

        await db.SaveChangesAsync(cancellationToken);
    }

    private async Task RestockAsync(Domain.Orders.Order? order, CancellationToken cancellationToken)
    {
        if (order is null) return;

        var variantIds = order.Items.Select(i => i.ProductVariantId).ToList();

        var products = await db.Products
            .Where(p => p.Variants.Any(v => variantIds.Contains(v.Id)))
            .ToListAsync(cancellationToken);

        var variantsById = products
            .SelectMany(p => p.Variants)
            .Where(v => variantIds.Contains(v.Id))
            .ToDictionary(v => v.Id);

        foreach (var item in order.Items)
        {
            if (variantsById.TryGetValue(item.ProductVariantId, out var variant))
                variant.Restock(item.Quantity);
        }
    }
}
