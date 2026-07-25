using NikaFitness.Domain.Common;
using NikaFitness.Domain.ValueObjects;

namespace NikaFitness.Domain.Tests;

public class AddressTests
{
    private static Address Valid() => new(
        fullName: "Jane Doe",
        line1: "123 Ngong Rd",
        city: "Nairobi",
        country: "Kenya",
        phoneNumber: "254712345678");

    [Fact]
    public void Constructs_With_Required_Fields()
    {
        var address = Valid();

        Assert.Equal("Jane Doe", address.FullName);
        Assert.Equal("123 Ngong Rd", address.Line1);
        Assert.Equal("Nairobi", address.City);
        Assert.Equal("Kenya", address.Country);
        Assert.Equal("254712345678", address.PhoneNumber);
        Assert.Null(address.Line2);
        Assert.Null(address.PostalCode);
    }

    [Fact]
    public void Keeps_Optional_Fields_When_Supplied()
    {
        var address = new Address("Jane Doe", "123 Ngong Rd", "Nairobi", "Kenya",
            "254712345678", line2: "Apt 4", postalCode: "00100");

        Assert.Equal("Apt 4", address.Line2);
        Assert.Equal("00100", address.PostalCode);
    }

    [Theory]
    [InlineData("", "l1", "city", "country", "phone")]
    [InlineData("name", "", "city", "country", "phone")]
    [InlineData("name", "l1", "", "country", "phone")]
    [InlineData("name", "l1", "city", "", "phone")]
    [InlineData("name", "l1", "city", "country", "")]
    public void Rejects_Missing_Required_Fields(
        string fullName, string line1, string city, string country, string phone)
    {
        Assert.Throws<DomainException>(() => new Address(fullName, line1, city, country, phone));
    }
}
