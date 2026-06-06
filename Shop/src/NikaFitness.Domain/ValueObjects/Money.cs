using NikaFitness.Domain.Common;

namespace NikaFitness.Domain.ValueObjects;

/// <summary>
/// An immutable monetary amount. Defaults to Kenyan Shillings (KES) since the
/// primary payment rail is M-Pesa.
/// </summary>
public sealed record Money
{
    public const string DefaultCurrency = "KES";

    public decimal Amount { get; }
    public string Currency { get; }

    public Money(decimal amount, string currency = DefaultCurrency)
    {
        if (amount < 0)
            throw new DomainException("Money amount cannot be negative.");
        if (string.IsNullOrWhiteSpace(currency))
            throw new DomainException("Currency is required.");

        Amount = decimal.Round(amount, 2, MidpointRounding.AwayFromZero);
        Currency = currency.ToUpperInvariant();
    }

    public static Money Zero(string currency = DefaultCurrency) => new(0, currency);

    public Money Add(Money other)
    {
        EnsureSameCurrency(other);
        return new Money(Amount + other.Amount, Currency);
    }

    public Money Multiply(int quantity) => new(Amount * quantity, Currency);

    private void EnsureSameCurrency(Money other)
    {
        if (Currency != other.Currency)
            throw new DomainException($"Cannot operate on {Currency} and {other.Currency}.");
    }

    public override string ToString() => $"{Currency} {Amount:N2}";
}
