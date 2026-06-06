using NikaFitness.Domain.Common;
using NikaFitness.Domain.ValueObjects;

namespace NikaFitness.Domain.Catalog;

/// <summary>
/// A purchasable variation of a product (e.g. size M, colour Black), carrying its
/// own SKU, price and stock level.
/// </summary>
public sealed class ProductVariant : Entity
{
    public Guid ProductId { get; private set; }
    public string Sku { get; private set; } = default!;
    public string Name { get; private set; } = default!;
    public Money Price { get; private set; } = default!;
    public int StockQuantity { get; private set; }

    private ProductVariant() { }

    internal ProductVariant(string sku, string name, Money price, int stockQuantity)
    {
        if (string.IsNullOrWhiteSpace(sku)) throw new DomainException("SKU is required.");
        if (string.IsNullOrWhiteSpace(name)) throw new DomainException("Variant name is required.");
        if (stockQuantity < 0) throw new DomainException("Stock quantity cannot be negative.");

        Sku = sku.Trim().ToUpperInvariant();
        Name = name.Trim();
        Price = price;
        StockQuantity = stockQuantity;
    }

    public bool HasStock(int quantity) => StockQuantity >= quantity;

    public void UpdatePrice(Money price) => Price = price;

    public void Restock(int quantity)
    {
        if (quantity <= 0) throw new DomainException("Restock quantity must be positive.");
        StockQuantity += quantity;
    }

    /// <summary>Reserves stock for an order. Throws if insufficient.</summary>
    public void Reserve(int quantity)
    {
        if (quantity <= 0) throw new DomainException("Reserve quantity must be positive.");
        if (!HasStock(quantity))
            throw new DomainException($"Insufficient stock for SKU {Sku}. Requested {quantity}, available {StockQuantity}.");

        StockQuantity -= quantity;
    }
}
