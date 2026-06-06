using NikaFitness.Domain.Common;
using NikaFitness.Domain.ValueObjects;

namespace NikaFitness.Domain.Catalog;

/// <summary>
/// A sellable product in the Nika Fitness catalogue. The product aggregate owns
/// its variants and guards their consistency.
/// </summary>
public sealed class Product : AggregateRoot
{
    private readonly List<ProductVariant> _variants = new();
    private readonly List<string> _imageUrls = new();

    public string Name { get; private set; } = default!;
    public Slug Slug { get; private set; } = default!;
    public string Description { get; private set; } = default!;
    public Guid CategoryId { get; private set; }
    public bool IsPublished { get; private set; }
    public DateTime CreatedAtUtc { get; private set; } = DateTime.UtcNow;

    public IReadOnlyList<ProductVariant> Variants => _variants.AsReadOnly();
    public IReadOnlyList<string> ImageUrls => _imageUrls.AsReadOnly();

    private Product() { }

    public Product(string name, string description, Guid categoryId)
    {
        if (string.IsNullOrWhiteSpace(description))
            throw new DomainException("Product description is required.");
        if (categoryId == Guid.Empty)
            throw new DomainException("A product must belong to a category.");

        Rename(name);
        Description = description;
        CategoryId = categoryId;
    }

    public void Rename(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new DomainException("Product name is required.");

        Name = name.Trim();
        Slug = Slug.Create(Name);
    }

    public void UpdateDescription(string description)
    {
        if (string.IsNullOrWhiteSpace(description))
            throw new DomainException("Product description is required.");
        Description = description;
    }

    public ProductVariant AddVariant(string sku, string name, Money price, int stockQuantity)
    {
        if (_variants.Any(v => v.Sku.Equals(sku, StringComparison.OrdinalIgnoreCase)))
            throw new DomainException($"A variant with SKU {sku} already exists for this product.");

        var variant = new ProductVariant(sku, name, price, stockQuantity);
        _variants.Add(variant);
        return variant;
    }

    public void AddImage(string url)
    {
        if (string.IsNullOrWhiteSpace(url))
            throw new DomainException("Image URL is required.");
        _imageUrls.Add(url);
    }

    public ProductVariant GetVariant(Guid variantId)
        => _variants.FirstOrDefault(v => v.Id == variantId)
           ?? throw new DomainException("Variant not found on this product.");

    public void Publish()
    {
        if (_variants.Count == 0)
            throw new DomainException("Cannot publish a product without at least one variant.");
        IsPublished = true;
    }

    public void Unpublish() => IsPublished = false;
}
