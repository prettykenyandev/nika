using NikaFitness.Domain.Payments;

namespace NikaFitness.Application.Common.Interfaces;

/// <summary>
/// Request to initiate a payment with an external provider. Provider-agnostic so a
/// card gateway can be added later without touching order logic.
/// </summary>
public sealed record PaymentInitiationRequest(
    Guid PaymentId,
    Guid OrderId,
    string OrderNumber,
    decimal Amount,
    string Currency,
    string PhoneNumber);

public sealed record PaymentInitiationResult(
    bool Success,
    string? ProviderRequestId,
    string? CustomerMessage,
    string? Error)
{
    public static PaymentInitiationResult Ok(string providerRequestId, string customerMessage)
        => new(true, providerRequestId, customerMessage, null);

    public static PaymentInitiationResult Failed(string error)
        => new(false, null, null, error);
}

/// <summary>
/// A payment gateway. Each implementation handles one <see cref="PaymentMethod"/>.
/// </summary>
public interface IPaymentGateway
{
    PaymentMethod Method { get; }

    Task<PaymentInitiationResult> InitiateAsync(
        PaymentInitiationRequest request,
        CancellationToken cancellationToken);
}
