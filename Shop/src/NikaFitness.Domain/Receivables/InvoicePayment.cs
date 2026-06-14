using NikaFitness.Domain.Common;
using NikaFitness.Domain.ValueObjects;

namespace NikaFitness.Domain.Receivables;

/// <summary>A receipt recorded against an <see cref="Invoice"/> (money entering the business).</summary>
public sealed class InvoicePayment : Entity
{
    public Guid InvoiceId { get; private set; }
    public Money Amount { get; private set; } = default!;
    public DateOnly ReceivedOn { get; private set; }
    public string Method { get; private set; } = default!;
    public string? Reference { get; private set; }

    private InvoicePayment() { }

    internal InvoicePayment(Money amount, DateOnly receivedOn, string method, string? reference)
    {
        if (amount.Amount <= 0)
            throw new DomainException("Receipt amount must be positive.");
        if (string.IsNullOrWhiteSpace(method))
            throw new DomainException("Payment method is required.");

        Amount = amount;
        ReceivedOn = receivedOn;
        Method = method.Trim();
        Reference = string.IsNullOrWhiteSpace(reference) ? null : reference.Trim();
    }
}
