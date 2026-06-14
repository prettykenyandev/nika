using NikaFitness.Domain.Common;

namespace NikaFitness.Domain.ValueObjects;

/// <summary>
/// An immutable tax rate expressed as a percentage (e.g. 16 for Kenyan VAT).
/// Used to compute tax on monetary amounts for invoices and bills.
/// </summary>
public sealed record TaxRate
{
    /// <summary>Kenya standard VAT.</summary>
    public const decimal DefaultPercent = 16m;

    public decimal Percent { get; }

    public TaxRate(decimal percent)
    {
        if (percent < 0)
            throw new DomainException("Tax rate cannot be negative.");
        if (percent > 100)
            throw new DomainException("Tax rate cannot exceed 100%.");

        Percent = decimal.Round(percent, 2, MidpointRounding.AwayFromZero);
    }

    public static TaxRate Zero => new(0m);
    public static TaxRate Default => new(DefaultPercent);

    /// <summary>Returns the tax portion for a net (tax-exclusive) amount.</summary>
    public Money TaxOn(Money netAmount) =>
        new(netAmount.Amount * Percent / 100m, netAmount.Currency);

    public override string ToString() => $"{Percent:N2}%";
}
