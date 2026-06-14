namespace NikaFitness.Domain.Purchasing;

/// <summary>Lifecycle states of a <see cref="PurchaseOrder"/>.</summary>
public enum PurchaseOrderStatus
{
    Draft,
    Sent,
    PartiallyReceived,
    Received,
    Closed,
    Cancelled
}
