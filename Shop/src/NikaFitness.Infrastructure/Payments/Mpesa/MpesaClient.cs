using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace NikaFitness.Infrastructure.Payments.Mpesa;

/// <summary>
/// Thin typed client over the Daraja REST API. Handles OAuth token acquisition
/// (cached until shortly before expiry) and the STK Push request.
/// </summary>
public sealed class MpesaClient(
    HttpClient httpClient,
    IOptions<MpesaOptions> options,
    ILogger<MpesaClient> logger)
{
    private readonly MpesaOptions _options = options.Value;
    private readonly SemaphoreSlim _tokenLock = new(1, 1);

    private string? _cachedToken;
    private DateTimeOffset _tokenExpiresAt = DateTimeOffset.MinValue;

    public async Task<StkPushResult> InitiateStkPushAsync(
        long amount,
        string phoneNumber,
        string accountReference,
        string transactionDescription,
        CancellationToken cancellationToken)
    {
        var token = await GetAccessTokenAsync(cancellationToken);
        var timestamp = DateTime.UtcNow.ToString("yyyyMMddHHmmss");
        var password = Convert.ToBase64String(
            Encoding.UTF8.GetBytes(_options.BusinessShortCode + _options.Passkey + timestamp));

        var request = new StkPushRequest(
            _options.BusinessShortCode,
            password,
            timestamp,
            _options.TransactionType,
            amount,
            phoneNumber,
            _options.BusinessShortCode,
            phoneNumber,
            _options.CallbackUrl,
            accountReference,
            transactionDescription);

        using var message = new HttpRequestMessage(HttpMethod.Post, "/mpesa/stkpush/v1/processrequest")
        {
            Content = JsonContent.Create(request)
        };
        message.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await httpClient.SendAsync(message, cancellationToken);
        var body = await response.Content.ReadFromJsonAsync<StkPushResponse>(cancellationToken);

        if (!response.IsSuccessStatusCode || body is null)
        {
            var error = body?.ErrorMessage ?? body?.ResponseDescription ?? response.ReasonPhrase ?? "M-Pesa request failed.";
            logger.LogWarning("STK push failed: {Error}", error);
            return StkPushResult.Failed(error);
        }

        if (body.ResponseCode != "0" || string.IsNullOrEmpty(body.CheckoutRequestId))
        {
            var error = body.ResponseDescription ?? body.ErrorMessage ?? "M-Pesa rejected the request.";
            logger.LogWarning("STK push rejected: {Error}", error);
            return StkPushResult.Failed(error);
        }

        return StkPushResult.Ok(
            body.CheckoutRequestId!,
            body.CustomerMessage ?? "Enter your M-Pesa PIN to complete payment.");
    }

    private async Task<string> GetAccessTokenAsync(CancellationToken cancellationToken)
    {
        if (_cachedToken is not null && DateTimeOffset.UtcNow < _tokenExpiresAt)
            return _cachedToken;

        await _tokenLock.WaitAsync(cancellationToken);
        try
        {
            if (_cachedToken is not null && DateTimeOffset.UtcNow < _tokenExpiresAt)
                return _cachedToken;

            var credentials = Convert.ToBase64String(
                Encoding.UTF8.GetBytes($"{_options.ConsumerKey}:{_options.ConsumerSecret}"));

            using var request = new HttpRequestMessage(
                HttpMethod.Get, "/oauth/v1/generate?grant_type=client_credentials");
            request.Headers.Authorization = new AuthenticationHeaderValue("Basic", credentials);

            var response = await httpClient.SendAsync(request, cancellationToken);
            response.EnsureSuccessStatusCode();

            var token = await response.Content.ReadFromJsonAsync<MpesaTokenResponse>(cancellationToken)
                ?? throw new InvalidOperationException("Empty M-Pesa token response.");

            _cachedToken = token.AccessToken;
            var lifetime = int.TryParse(token.ExpiresIn, out var seconds) ? seconds : 3599;
            _tokenExpiresAt = DateTimeOffset.UtcNow.AddSeconds(lifetime - 60);

            return _cachedToken;
        }
        finally
        {
            _tokenLock.Release();
        }
    }
}

public sealed record StkPushResult(bool Success, string? CheckoutRequestId, string? CustomerMessage, string? Error)
{
    public static StkPushResult Ok(string checkoutRequestId, string customerMessage)
        => new(true, checkoutRequestId, customerMessage, null);

    public static StkPushResult Failed(string error) => new(false, null, null, error);
}
