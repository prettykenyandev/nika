namespace NikaFitness.Domain.Orders;

public enum OrderStatus
{
    /// <summary>Order created, awaiting payment confirmation (e.g. M-Pesa STK push).</summary>
    PendingPayment = 0,
    Paid = 1,
    Fulfilled = 2,
    Cancelled = 3,
    PaymentFailed = 4
}
