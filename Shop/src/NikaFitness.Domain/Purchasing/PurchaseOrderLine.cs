using NikaFitness.Domain.Common;
using NikaFitness.Domain.ValueObjects;

namespace NikaFitness.Domain.Purchasing;

/// <summary>A single ordered line on a <see cref="PurchaseOrder"/>.</summary>
public sealed class PurchaseOrderLine : Entity
{
    public Guid PurchaseOrderId { get; private set; }

    /// <summary>The product variant being restocked, when this line maps to inventory.</summary>
    public Guid? ProductVariantId { get; private set; }
    public string Description { get; private set; } = default!;
    public string? Sku { get; private set; }
    public decimal Quantity { get; private set; }
    public decimal QuantityReceived { get; private set; }
    public Money UnitCost { get; private set; } = default!;
    public TaxRate TaxRate { get; private set; } = TaxRate.Zero;

    public decimal QuantityOutstanding => Math.Max(0, Quantity - QuantityReceived);
    public bool IsFullyReceived => QuantityReceived >= Quantity;

    public Money LineNet => new(UnitCost.Amount * Quantity, UnitCost.Currency);
    public Money LineTax => TaxRate.TaxOn(LineNet);
    public Money LineTotal => LineNet.Add(LineTax);

    private PurchaseOrderLine() { }

    internal PurchaseOrderLine(
        Guid? productVariantId,
        string description,
        string? sku,
        decimal quantity,
        Money unitCost,
        TaxRate taxRate)
    {
        if (string.IsNullOrWhiteSpace(description))
            throw new DomainException("Purchase order line needs a description.");
        if (quantity <= 0)
            throw new DomainException("Purchase order line quantity must be positive.");

        ProductVariantId = productVariantId;
        Description = description.Trim();
        Sku = string.IsNullOrWhiteSpace(sku) ? null : sku.Trim();
        Quantity = quantity;
        QuantityReceived = 0;
        UnitCost = unitCost;
        TaxRate = taxRate;
    }

    /// <summary>Records a receipt of <paramref name="quantity"/> units. Returns the quantity actually received.</summary>
    internal decimal Receive(decimal quantity)
    {
        if (quantity <= 0)
            throw new DomainException("Received quantity must be positive.");
        if (quantity > QuantityOutstanding)
            throw new DomainException(
                $"Cannot receive {quantity} of '{Description}'; only {QuantityOutstanding} outstanding.");

        QuantityReceived += quantity;
        return quantity;
    }
}
