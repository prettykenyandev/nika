using NikaFitness.Domain.Common;
using NikaFitness.Domain.ValueObjects;

namespace NikaFitness.Domain.Tests;

public class MoneyTests
{
    [Fact]
    public void Defaults_To_Kes_And_Rounds_Away_From_Zero()
    {
        var money = new Money(10.125m);

        Assert.Equal("KES", money.Currency);
        Assert.Equal(10.13m, money.Amount);
    }

    [Fact]
    public void Uppercases_Currency()
    {
        Assert.Equal("USD", new Money(5m, "usd").Currency);
    }

    [Theory]
    [InlineData(-0.01)]
    [InlineData(-100)]
    public void Rejects_Negative_Amounts(decimal amount)
    {
        Assert.Throws<DomainException>(() => new Money(amount));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Rejects_Blank_Currency(string currency)
    {
        Assert.Throws<DomainException>(() => new Money(1m, currency));
    }

    [Fact]
    public void Zero_Is_Zero_In_Given_Currency()
    {
        var zero = Money.Zero("EUR");
        Assert.Equal(0m, zero.Amount);
        Assert.Equal("EUR", zero.Currency);
    }

    [Fact]
    public void Add_Sums_Same_Currency()
    {
        var result = new Money(10m).Add(new Money(5.50m));
        Assert.Equal(15.50m, result.Amount);
    }

    [Fact]
    public void Add_Rejects_Currency_Mismatch()
    {
        Assert.Throws<DomainException>(() => new Money(10m, "KES").Add(new Money(5m, "USD")));
    }

    [Fact]
    public void Multiply_Scales_By_Quantity()
    {
        Assert.Equal(30m, new Money(10m).Multiply(3).Amount);
    }

    [Fact]
    public void ToString_Formats_With_Two_Decimals()
    {
        Assert.Equal("KES 1,234.50", new Money(1234.5m).ToString());
    }

    [Fact]
    public void Records_Compare_By_Value()
    {
        Assert.Equal(new Money(10m), new Money(10m));
    }
}
