namespace NikaFitness.Domain.Receivables;

/// <summary>Lifecycle of a customer invoice (accounts receivable).</summary>
public enum InvoiceStatus
{
    /// <summary>Drafted but not yet issued to the customer.</summary>
    Draft = 0,
    /// <summary>Issued/sent to the customer and awaiting payment.</summary>
    Sent = 1,
    /// <summary>Part of the balance has been received.</summary>
    PartiallyPaid = 2,
    /// <summary>Fully settled.</summary>
    Paid = 3,
    /// <summary>Voided; no longer collectable.</summary>
    Void = 4
}
