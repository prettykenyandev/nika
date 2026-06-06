namespace NikaFitness.Application.Carts.Models;

/// <summary>A single line stored in a shopping cart. Prices are resolved at read
/// time from the catalogue so the cart always reflects current pricing.</summary>
public sealed class CartLine
{
    public Guid ProductId { get; set; }
    public Guid ProductVariantId { get; set; }
    public int Quantity { get; set; }
}

/// <summary>
/// A shopping cart persisted in a fast key/value store (Redis). Kept out of the
/// relational model because carts are high-churn, short-lived, and don't need
/// transactional guarantees until checkout.
/// </summary>
public sealed class Cart
{
    public string Id { get; set; } = default!;
    public List<CartLine> Lines { get; set; } = new();

    public void AddOrIncrement(Guid productId, Guid variantId, int quantity)
    {
        var existing = Lines.FirstOrDefault(l => l.ProductVariantId == variantId);
        if (existing is null)
        {
            Lines.Add(new CartLine
            {
                ProductId = productId,
                ProductVariantId = variantId,
                Quantity = quantity
            });
        }
        else
        {
            existing.Quantity += quantity;
        }
    }

    public void SetQuantity(Guid variantId, int quantity)
    {
        var line = Lines.FirstOrDefault(l => l.ProductVariantId == variantId);
        if (line is null) return;

        if (quantity <= 0)
            Lines.Remove(line);
        else
            line.Quantity = quantity;
    }

    public void Remove(Guid variantId)
        => Lines.RemoveAll(l => l.ProductVariantId == variantId);
}
