using NikaFitness.Domain.Common;
using NikaFitness.Domain.ValueObjects;

namespace NikaFitness.Domain.Purchasing;

/// <summary>
/// A purchase order raised against a <see cref="Vendor"/>. Owns its lines and is the single
/// place where ordering, goods-receipt and closing transitions are enforced. Receiving stock
/// and generating a bill are coordinated by the application layer using the data exposed here.
/// </summary>
public sealed class PurchaseOrder : AggregateRoot
{
    private readonly List<PurchaseOrderLine> _lines = new();

    public string PoNumber { get; private set; } = default!;
    public Guid VendorId { get; private set; }
    public string VendorName { get; private set; } = default!;
    public DateOnly OrderDate { get; private set; }
    public DateOnly? ExpectedDate { get; private set; }
    public string Currency { get; private set; } = Money.DefaultCurrency;
    public PurchaseOrderStatus Status { get; private set; }
    public string? Notes { get; private set; }
    public DateTime CreatedAtUtc { get; private set; } = DateTime.UtcNow;

    /// <summary>The bill generated when this PO was closed, if any.</summary>
    public Guid? GeneratedBillId { get; private set; }

    public IReadOnlyList<PurchaseOrderLine> Lines => _lines.AsReadOnly();

    public Money Subtotal => _lines.Aggregate(
        Money.Zero(Currency), (running, line) => running.Add(line.LineNet));

    public Money TaxTotal => _lines.Aggregate(
        Money.Zero(Currency), (running, line) => running.Add(line.LineTax));

    public Money Total => Subtotal.Add(TaxTotal);

    private PurchaseOrder() { }

    public static PurchaseOrder Create(
        string poNumber,
        Guid vendorId,
        string vendorName,
        DateOnly orderDate,
        DateOnly? expectedDate,
        string currency = Money.DefaultCurrency,
        string? notes = null)
    {
        if (string.IsNullOrWhiteSpace(poNumber))
            throw new DomainException("Purchase order number is required.");
        if (vendorId == Guid.Empty)
            throw new DomainException("A purchase order must reference a vendor.");
        if (string.IsNullOrWhiteSpace(vendorName))
            throw new DomainException("Vendor name is required.");
        if (expectedDate is { } expected && expected < orderDate)
            throw new DomainException("Expected date cannot be before the order date.");

        return new PurchaseOrder
        {
            PoNumber = poNumber.Trim(),
            VendorId = vendorId,
            VendorName = vendorName.Trim(),
            OrderDate = orderDate,
            ExpectedDate = expectedDate,
            Currency = currency.ToUpperInvariant(),
            Notes = Clean(notes),
            Status = PurchaseOrderStatus.Draft
        };
    }

    public PurchaseOrderLine AddLine(
        Guid? productVariantId,
        string description,
        string? sku,
        decimal quantity,
        Money unitCost,
        TaxRate taxRate)
    {
        EnsureEditable();
        if (unitCost.Currency != Currency)
            throw new DomainException("All purchase order lines must use the order currency.");

        var line = new PurchaseOrderLine(productVariantId, description, sku, quantity, unitCost, taxRate);
        _lines.Add(line);
        return line;
    }

    public void ClearLines()
    {
        EnsureEditable();
        _lines.Clear();
    }

    public void UpdateDetails(DateOnly orderDate, DateOnly? expectedDate, string? notes)
    {
        EnsureEditable();
        if (expectedDate is { } expected && expected < orderDate)
            throw new DomainException("Expected date cannot be before the order date.");

        OrderDate = orderDate;
        ExpectedDate = expectedDate;
        Notes = Clean(notes);
    }

    /// <summary>Issues a draft purchase order to the vendor.</summary>
    public void Send()
    {
        if (Status != PurchaseOrderStatus.Draft)
            throw new DomainException($"Only draft purchase orders can be sent (was {Status}).");
        if (_lines.Count == 0)
            throw new DomainException("Cannot send a purchase order with no lines.");

        Status = PurchaseOrderStatus.Sent;
    }

    /// <summary>
    /// Records goods receipts against the given lines and advances the status. Returns the
    /// per-variant quantities that should be restocked (whole units only) so the caller can
    /// update inventory.
    /// </summary>
    public IReadOnlyList<StockReceipt> Receive(IReadOnlyList<ReceiptItem> receipts)
    {
        if (Status is not (PurchaseOrderStatus.Sent or PurchaseOrderStatus.PartiallyReceived))
            throw new DomainException($"Goods can only be received on a sent purchase order (was {Status}).");
        if (receipts is null || receipts.Count == 0)
            throw new DomainException("Specify at least one line to receive.");

        var restocks = new List<StockReceipt>();
        foreach (var receipt in receipts)
        {
            var line = _lines.FirstOrDefault(l => l.Id == receipt.LineId)
                ?? throw new DomainException("Receipt references a line that is not on this purchase order.");

            var received = line.Receive(receipt.Quantity);
            if (line.ProductVariantId is { } variantId)
                restocks.Add(new StockReceipt(variantId, (int)received));
        }

        Status = _lines.All(l => l.IsFullyReceived)
            ? PurchaseOrderStatus.Received
            : PurchaseOrderStatus.PartiallyReceived;

        return restocks;
    }

    public void RecordGeneratedBill(Guid billId)
    {
        GeneratedBillId = billId;
    }

    /// <summary>Closes a (partially) received purchase order.</summary>
    public void Close()
    {
        if (Status is not (PurchaseOrderStatus.Received or PurchaseOrderStatus.PartiallyReceived))
            throw new DomainException($"Only received purchase orders can be closed (was {Status}).");

        Status = PurchaseOrderStatus.Closed;
    }

    public void Cancel()
    {
        if (Status is PurchaseOrderStatus.Closed or PurchaseOrderStatus.Cancelled)
            throw new DomainException($"This purchase order can no longer be cancelled ({Status}).");
        if (_lines.Any(l => l.QuantityReceived > 0))
            throw new DomainException("Cannot cancel a purchase order that has already received goods.");

        Status = PurchaseOrderStatus.Cancelled;
    }

    private void EnsureEditable()
    {
        if (Status is not PurchaseOrderStatus.Draft)
            throw new DomainException($"Purchase order {PoNumber} can no longer be edited ({Status}).");
    }

    private static string? Clean(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}

/// <summary>An instruction to receive a quantity against a specific PO line.</summary>
public readonly record struct ReceiptItem(Guid LineId, decimal Quantity);

/// <summary>A quantity of stock to add back to a product variant after goods receipt.</summary>
public readonly record struct StockReceipt(Guid ProductVariantId, int Quantity);
