using NikaFitness.Domain.Common;
using NikaFitness.Domain.ValueObjects;

namespace NikaFitness.Domain.Expenses;

/// <summary>
/// A grouping for business costs (e.g. "Rent", "Utilities", "Salaries", "Stock purchases").
/// Used to classify <see cref="BillLine"/> items for reporting.
/// </summary>
public sealed class ExpenseCategory : AggregateRoot
{
    public string Name { get; private set; } = default!;
    public Slug Slug { get; private set; } = default!;
    public string? Description { get; private set; }

    private ExpenseCategory() { }

    public ExpenseCategory(string name, string? description = null)
    {
        Rename(name);
        Description = description;
    }

    public void Rename(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new DomainException("Expense category name is required.");

        Name = name.Trim();
        Slug = Slug.Create(Name);
    }

    public void UpdateDescription(string? description) => Description = description;
}
