using NikaFitness.Domain.Common;
using NikaFitness.Domain.Payments.Events;
using NikaFitness.Domain.ValueObjects;

namespace NikaFitness.Domain.Payments;

/// <summary>
/// Tracks a single payment attempt against an order. M-Pesa is asynchronous, so a
/// payment moves Pending -> Processing (STK push sent) -> Succeeded/Failed once the
/// provider calls our webhook. Designed to be idempotent: confirming twice is a no-op.
/// </summary>
public sealed class Payment : AggregateRoot
{
    public Guid OrderId { get; private set; }
    public PaymentMethod Method { get; private set; }
    public PaymentStatus Status { get; private set; }
    public Money Amount { get; private set; } = default!;

    /// <summary>The payer phone (M-Pesa) in MSISDN format, e.g. 2547XXXXXXXX.</summary>
    public string PayerReference { get; private set; } = default!;

    /// <summary>Provider request id (e.g. M-Pesa CheckoutRequestID) used to correlate the webhook.</summary>
    public string? ProviderRequestId { get; private set; }

    /// <summary>Final provider transaction id (e.g. M-Pesa receipt number).</summary>
    public string? ProviderReference { get; private set; }

    public string? FailureReason { get; private set; }
    public DateTime CreatedAtUtc { get; private set; } = DateTime.UtcNow;
    public DateTime? CompletedAtUtc { get; private set; }

    private Payment() { }

    public static Payment ForOrder(Guid orderId, Money amount, PaymentMethod method, string payerReference)
    {
        if (orderId == Guid.Empty) throw new DomainException("Payment must reference an order.");
        if (string.IsNullOrWhiteSpace(payerReference)) throw new DomainException("Payer reference is required.");

        return new Payment
        {
            OrderId = orderId,
            Amount = amount,
            Method = method,
            PayerReference = payerReference,
            Status = PaymentStatus.Pending
        };
    }

    /// <summary>Records that the provider accepted the request and is awaiting confirmation.</summary>
    public void MarkProcessing(string providerRequestId)
    {
        if (string.IsNullOrWhiteSpace(providerRequestId))
            throw new DomainException("Provider request id is required.");
        if (Status is not PaymentStatus.Pending)
            throw new DomainException($"Cannot start processing a {Status} payment.");

        ProviderRequestId = providerRequestId;
        Status = PaymentStatus.Processing;
    }

    public void MarkSucceeded(string providerReference)
    {
        if (Status is PaymentStatus.Succeeded) return; // idempotent
        if (Status is PaymentStatus.Failed or PaymentStatus.Cancelled)
            throw new DomainException($"Cannot complete a {Status} payment.");

        ProviderReference = providerReference;
        Status = PaymentStatus.Succeeded;
        CompletedAtUtc = DateTime.UtcNow;
        Raise(new PaymentSucceededEvent(Id, OrderId, providerReference));
    }

    public void MarkFailed(string reason)
    {
        if (Status is PaymentStatus.Failed) return; // idempotent
        if (Status is PaymentStatus.Succeeded)
            throw new DomainException("Cannot fail an already succeeded payment.");

        FailureReason = reason;
        Status = PaymentStatus.Failed;
        CompletedAtUtc = DateTime.UtcNow;
        Raise(new PaymentFailedEvent(Id, OrderId, reason));
    }
}
