using NikaFitness.Domain.Common;

namespace NikaFitness.Domain.Customers;

/// <summary>
/// A customer record — a durable identity for someone the business sells to, as opposed to
/// just an email captured on a single order. Orders and invoices reference this by id.
/// </summary>
public sealed class Customer : AggregateRoot
{
    public string Name { get; private set; } = default!;
    public string? Email { get; private set; }
    public string? Phone { get; private set; }
    public string? AddressLine1 { get; private set; }
    public string? City { get; private set; }
    public string? Country { get; private set; }
    public string? Notes { get; private set; }
    public bool IsActive { get; private set; } = true;
    public DateTime CreatedAtUtc { get; private set; } = DateTime.UtcNow;

    private Customer() { }

    public static Customer Create(
        string name,
        string? email = null,
        string? phone = null,
        string? addressLine1 = null,
        string? city = null,
        string? country = null,
        string? notes = null)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new DomainException("Customer name is required.");

        return new Customer
        {
            Name = name.Trim(),
            Email = CleanEmail(email),
            Phone = Clean(phone),
            AddressLine1 = Clean(addressLine1),
            City = Clean(city),
            Country = Clean(country),
            Notes = Clean(notes),
            IsActive = true
        };
    }

    public void UpdateDetails(
        string name,
        string? email,
        string? phone,
        string? addressLine1,
        string? city,
        string? country,
        string? notes)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new DomainException("Customer name is required.");

        Name = name.Trim();
        Email = CleanEmail(email);
        Phone = Clean(phone);
        AddressLine1 = Clean(addressLine1);
        City = Clean(city);
        Country = Clean(country);
        Notes = Clean(notes);
    }

    public void Activate() => IsActive = true;

    public void Deactivate() => IsActive = false;

    private static string? CleanEmail(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim().ToLowerInvariant();

    private static string? Clean(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
