namespace NikaFitness.Domain.Payments;

public enum PaymentStatus
{
    /// <summary>Payment record created, provider not yet contacted.</summary>
    Pending = 0,
    /// <summary>Provider accepted the request (e.g. STK push sent to the phone).</summary>
    Processing = 1,
    Succeeded = 2,
    Failed = 3,
    Cancelled = 4
}
