using NikaFitness.Domain.Common;
using NikaFitness.Domain.ValueObjects;

namespace NikaFitness.Domain.Expenses;

/// <summary>A single cost line on a <see cref="Bill"/>, optionally classified by category.</summary>
public sealed class BillLine : Entity
{
    public Guid BillId { get; private set; }
    public string Description { get; private set; } = default!;
    public Guid? ExpenseCategoryId { get; private set; }
    public decimal Quantity { get; private set; }
    public Money UnitCost { get; private set; } = default!;
    public TaxRate TaxRate { get; private set; } = TaxRate.Zero;

    public Money LineNet => new(UnitCost.Amount * Quantity, UnitCost.Currency);
    public Money LineTax => TaxRate.TaxOn(LineNet);
    public Money LineTotal => LineNet.Add(LineTax);

    private BillLine() { }

    internal BillLine(
        string description,
        Guid? expenseCategoryId,
        decimal quantity,
        Money unitCost,
        TaxRate taxRate)
    {
        if (string.IsNullOrWhiteSpace(description))
            throw new DomainException("Bill line needs a description.");
        if (quantity <= 0)
            throw new DomainException("Bill line quantity must be positive.");

        Description = description.Trim();
        ExpenseCategoryId = expenseCategoryId;
        Quantity = quantity;
        UnitCost = unitCost;
        TaxRate = taxRate;
    }
}
