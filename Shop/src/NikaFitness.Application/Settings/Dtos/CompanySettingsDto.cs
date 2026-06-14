namespace NikaFitness.Application.Settings.Dtos;

/// <summary>Company profile + finance defaults shown on the admin settings screen.</summary>
public sealed record CompanySettingsDto(
    string LegalName,
    string? TradingName,
    string? Email,
    string? Phone,
    string? TaxIdentifier,
    string? AddressLine1,
    string? AddressLine2,
    string? City,
    string? Country,
    string? LogoUrl,
    string Currency,
    decimal DefaultTaxPercent,
    string InvoiceNumberPrefix,
    string BillNumberPrefix,
    string PurchaseOrderNumberPrefix,
    string? InvoiceFooter,
    string? PaymentInstructions);
