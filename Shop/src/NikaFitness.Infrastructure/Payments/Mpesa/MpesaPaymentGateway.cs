using NikaFitness.Application.Common.Interfaces;
using NikaFitness.Domain.Payments;

namespace NikaFitness.Infrastructure.Payments.Mpesa;

/// <summary>Adapts <see cref="MpesaClient"/> to the provider-agnostic gateway contract.</summary>
public sealed class MpesaPaymentGateway(MpesaClient client) : IPaymentGateway
{
    public PaymentMethod Method => PaymentMethod.Mpesa;

    public async Task<PaymentInitiationResult> InitiateAsync(
        PaymentInitiationRequest request,
        CancellationToken cancellationToken)
    {
        // M-Pesa works in whole shillings.
        var amount = (long)Math.Ceiling(request.Amount);

        var result = await client.InitiateStkPushAsync(
            amount,
            request.PhoneNumber,
            accountReference: request.OrderNumber,
            transactionDescription: $"Nika Fitness {request.OrderNumber}",
            cancellationToken);

        return result.Success
            ? PaymentInitiationResult.Ok(result.CheckoutRequestId!, result.CustomerMessage!)
            : PaymentInitiationResult.Failed(result.Error ?? "Payment initiation failed.");
    }
}
