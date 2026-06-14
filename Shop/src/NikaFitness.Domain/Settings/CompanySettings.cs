using NikaFitness.Domain.Common;
using NikaFitness.Domain.ValueObjects;

namespace NikaFitness.Domain.Settings;

/// <summary>
/// Singleton company profile used to brand documents (invoices, bills, POs) and to
/// drive finance defaults: currency, tax rate, and document-number prefixes. There is
/// always exactly one row, identified by <see cref="SingletonId"/>.
/// </summary>
public sealed class CompanySettings : AggregateRoot
{
    /// <summary>Well-known id so the settings row is a true singleton.</summary>
    public static readonly Guid SingletonId = new("00000000-0000-0000-0000-0000000000C0");

    public string LegalName { get; private set; } = default!;
    public string? TradingName { get; private set; }
    public string? Email { get; private set; }
    public string? Phone { get; private set; }

    /// <summary>Tax identifier (e.g. KRA PIN in Kenya).</summary>
    public string? TaxIdentifier { get; private set; }

    public string? AddressLine1 { get; private set; }
    public string? AddressLine2 { get; private set; }
    public string? City { get; private set; }
    public string? Country { get; private set; }

    public string? LogoUrl { get; private set; }

    public string Currency { get; private set; } = Money.DefaultCurrency;
    public TaxRate DefaultTaxRate { get; private set; } = TaxRate.Default;

    public string InvoiceNumberPrefix { get; private set; } = "INV";
    public string BillNumberPrefix { get; private set; } = "BILL";
    public string PurchaseOrderNumberPrefix { get; private set; } = "PO";

    public string? InvoiceFooter { get; private set; }
    public string? PaymentInstructions { get; private set; }

    public DateTime UpdatedAtUtc { get; private set; } = DateTime.UtcNow;

    private CompanySettings() { }

    private CompanySettings(string legalName)
    {
        Id = SingletonId;
        LegalName = legalName;
    }

    /// <summary>Creates the default settings row for a fresh installation.</summary>
    public static CompanySettings CreateDefault() =>
        new("Nika Fitness")
        {
            TradingName = "Nika Fitness",
            Currency = Money.DefaultCurrency,
            DefaultTaxRate = TaxRate.Default,
            Country = "Kenya",
            City = "Nairobi",
        };

    public void Update(
        string legalName,
        string? tradingName,
        string? email,
        string? phone,
        string? taxIdentifier,
        string? addressLine1,
        string? addressLine2,
        string? city,
        string? country,
        string? logoUrl,
        string currency,
        TaxRate defaultTaxRate,
        string invoiceNumberPrefix,
        string billNumberPrefix,
        string purchaseOrderNumberPrefix,
        string? invoiceFooter,
        string? paymentInstructions)
    {
        if (string.IsNullOrWhiteSpace(legalName))
            throw new DomainException("Legal name is required.");
        if (string.IsNullOrWhiteSpace(currency))
            throw new DomainException("Currency is required.");
        if (string.IsNullOrWhiteSpace(invoiceNumberPrefix))
            throw new DomainException("Invoice number prefix is required.");
        if (string.IsNullOrWhiteSpace(billNumberPrefix))
            throw new DomainException("Bill number prefix is required.");
        if (string.IsNullOrWhiteSpace(purchaseOrderNumberPrefix))
            throw new DomainException("Purchase order number prefix is required.");

        LegalName = legalName.Trim();
        TradingName = Clean(tradingName);
        Email = Clean(email);
        Phone = Clean(phone);
        TaxIdentifier = Clean(taxIdentifier);
        AddressLine1 = Clean(addressLine1);
        AddressLine2 = Clean(addressLine2);
        City = Clean(city);
        Country = Clean(country);
        LogoUrl = Clean(logoUrl);
        Currency = currency.Trim().ToUpperInvariant();
        DefaultTaxRate = defaultTaxRate;
        InvoiceNumberPrefix = invoiceNumberPrefix.Trim().ToUpperInvariant();
        BillNumberPrefix = billNumberPrefix.Trim().ToUpperInvariant();
        PurchaseOrderNumberPrefix = purchaseOrderNumberPrefix.Trim().ToUpperInvariant();
        InvoiceFooter = Clean(invoiceFooter);
        PaymentInstructions = Clean(paymentInstructions);
        UpdatedAtUtc = DateTime.UtcNow;
    }

    private static string? Clean(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
