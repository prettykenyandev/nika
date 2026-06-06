namespace NikaFitness.Infrastructure.Payments.Mpesa;

/// <summary>
/// Configuration for Safaricom Daraja (M-Pesa) STK Push. Secrets should come from
/// user-secrets / environment variables, never source control.
/// </summary>
public sealed class MpesaOptions
{
    public const string SectionName = "Mpesa";

    /// <summary>Sandbox: https://sandbox.safaricom.co.ke — Production: https://api.safaricom.co.ke</summary>
    public string BaseUrl { get; set; } = "https://sandbox.safaricom.co.ke";

    public string ConsumerKey { get; set; } = string.Empty;
    public string ConsumerSecret { get; set; } = string.Empty;

    /// <summary>Lipa Na M-Pesa Online shortcode (Paybill/Till). Sandbox default: 174379.</summary>
    public string BusinessShortCode { get; set; } = "174379";

    /// <summary>LNM passkey issued by Safaricom.</summary>
    public string Passkey { get; set; } = string.Empty;

    /// <summary>CustomerPayBillOnline (Paybill) or CustomerBuyGoodsOnline (Till).</summary>
    public string TransactionType { get; set; } = "CustomerPayBillOnline";

    /// <summary>Publicly reachable URL Safaricom calls with the payment result.</summary>
    public string CallbackUrl { get; set; } = string.Empty;
}
