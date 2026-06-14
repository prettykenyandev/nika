using NikaFitness.Domain.Common;
using NikaFitness.Domain.ValueObjects;

namespace NikaFitness.Domain.Expenses;

/// <summary>A payment made against a <see cref="Bill"/> (money leaving the business).</summary>
public sealed class BillPayment : Entity
{
    public Guid BillId { get; private set; }
    public Money Amount { get; private set; } = default!;
    public DateOnly PaidOn { get; private set; }
    public string Method { get; private set; } = default!;
    public string? Reference { get; private set; }

    private BillPayment() { }

    internal BillPayment(Money amount, DateOnly paidOn, string method, string? reference)
    {
        if (amount.Amount <= 0)
            throw new DomainException("Payment amount must be positive.");
        if (string.IsNullOrWhiteSpace(method))
            throw new DomainException("Payment method is required.");

        Amount = amount;
        PaidOn = paidOn;
        Method = method.Trim();
        Reference = string.IsNullOrWhiteSpace(reference) ? null : reference.Trim();
    }
}
