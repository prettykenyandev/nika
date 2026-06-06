using NikaFitness.Domain.Common;

namespace NikaFitness.Domain.ValueObjects;

/// <summary>
/// A shipping/billing address captured at checkout time.
/// </summary>
public sealed record Address
{
    public string FullName { get; }
    public string Line1 { get; }
    public string? Line2 { get; }
    public string City { get; }
    public string? PostalCode { get; }
    public string Country { get; }
    public string PhoneNumber { get; }

    public Address(
        string fullName,
        string line1,
        string city,
        string country,
        string phoneNumber,
        string? line2 = null,
        string? postalCode = null)
    {
        if (string.IsNullOrWhiteSpace(fullName)) throw new DomainException("Full name is required.");
        if (string.IsNullOrWhiteSpace(line1)) throw new DomainException("Address line 1 is required.");
        if (string.IsNullOrWhiteSpace(city)) throw new DomainException("City is required.");
        if (string.IsNullOrWhiteSpace(country)) throw new DomainException("Country is required.");
        if (string.IsNullOrWhiteSpace(phoneNumber)) throw new DomainException("Phone number is required.");

        FullName = fullName;
        Line1 = line1;
        Line2 = line2;
        City = city;
        PostalCode = postalCode;
        Country = country;
        PhoneNumber = phoneNumber;
    }
}
