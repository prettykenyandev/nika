namespace NikaFitness.Domain.Payments;

public enum PaymentMethod
{
    Mpesa = 0,
    /// <summary>Cash taken at a point-of-sale till. Retained for historical sales; no longer offered.</summary>
    Cash = 1,
    /// <summary>Card payment captured on an external terminal. Settles immediately at the till.</summary>
    Card = 2
}
