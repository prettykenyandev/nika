namespace NikaFitness.Domain.Payments;

public enum PaymentMethod
{
    Mpesa = 0,
    /// <summary>Cash taken at a point-of-sale till. Settles immediately (no async provider).</summary>
    Cash = 1
}
