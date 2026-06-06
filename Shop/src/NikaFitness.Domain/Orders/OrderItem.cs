using NikaFitness.Domain.Common;
using NikaFitness.Domain.ValueObjects;

namespace NikaFitness.Domain.Orders;

/// <summary>
/// A line on an order. Captures an immutable snapshot of the product at purchase
/// time so later catalogue edits never change historical orders.
/// </summary>
public sealed class OrderItem : Entity
{
    public Guid OrderId { get; private set; }
    public Guid ProductId { get; private set; }
    public Guid ProductVariantId { get; private set; }
    public string ProductName { get; private set; } = default!;
    public string Sku { get; private set; } = default!;
    public Money UnitPrice { get; private set; } = default!;
    public int Quantity { get; private set; }

    public Money LineTotal => UnitPrice.Multiply(Quantity);

    private OrderItem() { }

    internal OrderItem(
        Guid productId,
        Guid productVariantId,
        string productName,
        string sku,
        Money unitPrice,
        int quantity)
    {
        if (quantity <= 0) throw new DomainException("Order item quantity must be positive.");

        ProductId = productId;
        ProductVariantId = productVariantId;
        ProductName = productName;
        Sku = sku;
        UnitPrice = unitPrice;
        Quantity = quantity;
    }
}
