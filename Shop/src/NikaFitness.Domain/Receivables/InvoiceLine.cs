using NikaFitness.Domain.Common;
using NikaFitness.Domain.ValueObjects;

namespace NikaFitness.Domain.Receivables;

/// <summary>A single billable line on an <see cref="Invoice"/>.</summary>
public sealed class InvoiceLine : Entity
{
    public Guid InvoiceId { get; private set; }
    public string Description { get; private set; } = default!;
    public decimal Quantity { get; private set; }
    public Money UnitPrice { get; private set; } = default!;
    public TaxRate TaxRate { get; private set; } = TaxRate.Zero;

    public Money LineNet => new(UnitPrice.Amount * Quantity, UnitPrice.Currency);
    public Money LineTax => TaxRate.TaxOn(LineNet);
    public Money LineTotal => LineNet.Add(LineTax);

    private InvoiceLine() { }

    internal InvoiceLine(
        string description,
        decimal quantity,
        Money unitPrice,
        TaxRate taxRate)
    {
        if (string.IsNullOrWhiteSpace(description))
            throw new DomainException("Invoice line needs a description.");
        if (quantity <= 0)
            throw new DomainException("Invoice line quantity must be positive.");

        Description = description.Trim();
        Quantity = quantity;
        UnitPrice = unitPrice;
        TaxRate = taxRate;
    }
}
