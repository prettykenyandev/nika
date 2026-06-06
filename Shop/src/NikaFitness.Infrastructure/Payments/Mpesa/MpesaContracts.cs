using System.Text.Json.Serialization;

namespace NikaFitness.Infrastructure.Payments.Mpesa;

internal sealed record MpesaTokenResponse(
    [property: JsonPropertyName("access_token")] string AccessToken,
    [property: JsonPropertyName("expires_in")] string ExpiresIn);

internal sealed record StkPushRequest(
    [property: JsonPropertyName("BusinessShortCode")] string BusinessShortCode,
    [property: JsonPropertyName("Password")] string Password,
    [property: JsonPropertyName("Timestamp")] string Timestamp,
    [property: JsonPropertyName("TransactionType")] string TransactionType,
    [property: JsonPropertyName("Amount")] long Amount,
    [property: JsonPropertyName("PartyA")] string PartyA,
    [property: JsonPropertyName("PartyB")] string PartyB,
    [property: JsonPropertyName("PhoneNumber")] string PhoneNumber,
    [property: JsonPropertyName("CallBackURL")] string CallBackUrl,
    [property: JsonPropertyName("AccountReference")] string AccountReference,
    [property: JsonPropertyName("TransactionDesc")] string TransactionDesc);

internal sealed record StkPushResponse(
    [property: JsonPropertyName("MerchantRequestID")] string? MerchantRequestId,
    [property: JsonPropertyName("CheckoutRequestID")] string? CheckoutRequestId,
    [property: JsonPropertyName("ResponseCode")] string? ResponseCode,
    [property: JsonPropertyName("ResponseDescription")] string? ResponseDescription,
    [property: JsonPropertyName("CustomerMessage")] string? CustomerMessage,
    [property: JsonPropertyName("errorMessage")] string? ErrorMessage);
