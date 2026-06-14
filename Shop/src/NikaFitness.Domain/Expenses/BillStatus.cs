namespace NikaFitness.Domain.Expenses;

/// <summary>Lifecycle of a supplier bill / expense (accounts payable).</summary>
public enum BillStatus
{
    /// <summary>Captured but not yet approved for payment.</summary>
    Draft = 0,
    /// <summary>Approved and awaiting payment.</summary>
    AwaitingPayment = 1,
    /// <summary>Part of the balance has been paid.</summary>
    PartiallyPaid = 2,
    /// <summary>Fully settled.</summary>
    Paid = 3,
    /// <summary>Voided; no longer payable.</summary>
    Cancelled = 4
}
