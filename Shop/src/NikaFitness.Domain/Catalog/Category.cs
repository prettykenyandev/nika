using NikaFitness.Domain.Common;
using NikaFitness.Domain.ValueObjects;

namespace NikaFitness.Domain.Catalog;

/// <summary>
/// A grouping of products, e.g. "Apparel" or "Supplements".
/// </summary>
public sealed class Category : AggregateRoot
{
    public string Name { get; private set; } = default!;
    public Slug Slug { get; private set; } = default!;
    public string? Description { get; private set; }

    private Category() { }

    public Category(string name, string? description = null)
    {
        Rename(name);
        Description = description;
    }

    public void Rename(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new DomainException("Category name is required.");

        Name = name.Trim();
        Slug = Slug.Create(Name);
    }

    public void UpdateDescription(string? description) => Description = description;
}
