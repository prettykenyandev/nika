using NikaFitness.Domain.Common;

namespace NikaFitness.Domain.Purchasing;

/// <summary>
/// A supplier the business buys stock and services from. Holds contact details and
/// default payment terms used to pre-fill purchase orders and bills.
/// </summary>
public sealed class Vendor : AggregateRoot
{
    public string Name { get; private set; } = default!;
    public string? ContactName { get; private set; }
    public string? Email { get; private set; }
    public string? Phone { get; private set; }
    public string? AddressLine1 { get; private set; }
    public string? City { get; private set; }
    public string? Country { get; private set; }
    public string? TaxIdentifier { get; private set; }

    /// <summary>Default number of days after a bill's issue date that payment is due.</summary>
    public int PaymentTermDays { get; private set; }
    public string? Notes { get; private set; }
    public bool IsActive { get; private set; } = true;
    public DateTime CreatedAtUtc { get; private set; } = DateTime.UtcNow;

    private Vendor() { }

    public static Vendor Create(
        string name,
        string? contactName = null,
        string? email = null,
        string? phone = null,
        string? addressLine1 = null,
        string? city = null,
        string? country = null,
        string? taxIdentifier = null,
        int paymentTermDays = 30,
        string? notes = null)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new DomainException("Vendor name is required.");
        if (paymentTermDays < 0)
            throw new DomainException("Payment terms cannot be negative.");

        return new Vendor
        {
            Name = name.Trim(),
            ContactName = Clean(contactName),
            Email = Clean(email),
            Phone = Clean(phone),
            AddressLine1 = Clean(addressLine1),
            City = Clean(city),
            Country = Clean(country),
            TaxIdentifier = Clean(taxIdentifier),
            PaymentTermDays = paymentTermDays,
            Notes = Clean(notes),
            IsActive = true
        };
    }

    public void UpdateDetails(
        string name,
        string? contactName,
        string? email,
        string? phone,
        string? addressLine1,
        string? city,
        string? country,
        string? taxIdentifier,
        int paymentTermDays,
        string? notes)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new DomainException("Vendor name is required.");
        if (paymentTermDays < 0)
            throw new DomainException("Payment terms cannot be negative.");

        Name = name.Trim();
        ContactName = Clean(contactName);
        Email = Clean(email);
        Phone = Clean(phone);
        AddressLine1 = Clean(addressLine1);
        City = Clean(city);
        Country = Clean(country);
        TaxIdentifier = Clean(taxIdentifier);
        PaymentTermDays = paymentTermDays;
        Notes = Clean(notes);
    }

    public void Activate() => IsActive = true;

    public void Deactivate() => IsActive = false;

    private static string? Clean(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
