using System.Text.Json;
using MediatR;
using NikaFitness.Application.Payments.Commands;

namespace NikaFitness.Api.Endpoints;

/// <summary>
/// Receives Safaricom Daraja STK push callbacks. Must be publicly reachable (use a tunnel
/// such as ngrok in development) and is intentionally anonymous — correlation is by
/// CheckoutRequestID, and the handler is idempotent.
/// </summary>
public static class PaymentWebhookEndpoints
{
    public static IEndpointRouteBuilder MapPaymentWebhookEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapPost("/api/payments/mpesa/callback", async (
            JsonElement payload,
            ISender sender,
            ILoggerFactory loggerFactory) =>
        {
            var logger = loggerFactory.CreateLogger("MpesaCallback");

            if (!payload.TryGetProperty("Body", out var body) ||
                !body.TryGetProperty("stkCallback", out var callback))
            {
                logger.LogWarning("Unrecognised M-Pesa callback payload.");
                // Always 200 so Safaricom does not retry indefinitely.
                return Results.Ok(new { ResultCode = 0, ResultDesc = "Ignored" });
            }

            var checkoutRequestId = callback.GetProperty("CheckoutRequestID").GetString();
            var resultCode = callback.GetProperty("ResultCode").GetInt32();
            var resultDesc = callback.TryGetProperty("ResultDesc", out var d) ? d.GetString() : null;

            if (string.IsNullOrEmpty(checkoutRequestId))
                return Results.Ok(new { ResultCode = 0, ResultDesc = "Ignored" });

            var success = resultCode == 0;
            var receipt = success ? ExtractReceipt(callback) : null;

            await sender.Send(new ConfirmMpesaPaymentCommand(
                checkoutRequestId, success, receipt, resultDesc));

            return Results.Ok(new { ResultCode = 0, ResultDesc = "Accepted" });
        }).WithTags("Payments");

        return app;
    }

    private static string? ExtractReceipt(JsonElement callback)
    {
        if (!callback.TryGetProperty("CallbackMetadata", out var metadata) ||
            !metadata.TryGetProperty("Item", out var items) ||
            items.ValueKind != JsonValueKind.Array)
        {
            return null;
        }

        foreach (var item in items.EnumerateArray())
        {
            if (item.TryGetProperty("Name", out var name) &&
                name.GetString() == "MpesaReceiptNumber" &&
                item.TryGetProperty("Value", out var value))
            {
                return value.ValueKind == JsonValueKind.String
                    ? value.GetString()
                    : value.ToString();
            }
        }

        return null;
    }
}
