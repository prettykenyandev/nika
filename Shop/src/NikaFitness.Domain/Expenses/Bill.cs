using NikaFitness.Domain.Common;
using NikaFitness.Domain.ValueObjects;

namespace NikaFitness.Domain.Expenses;

/// <summary>
/// The bill aggregate (accounts payable). Captures a supplier cost, its line items and
/// any payments made against it, and is the single place where AP state transitions and
/// balance calculations are enforced.
/// </summary>
public sealed class Bill : AggregateRoot
{
    private readonly List<BillLine> _lines = new();
    private readonly List<BillPayment> _payments = new();

    public string BillNumber { get; private set; } = default!;
    public string VendorName { get; private set; } = default!;
    public Guid? VendorId { get; private set; }

    /// <summary>The supplier's own invoice/reference number, if any.</summary>
    public string? SupplierReference { get; private set; }

    public DateOnly IssueDate { get; private set; }
    public DateOnly DueDate { get; private set; }
    public string Currency { get; private set; } = Money.DefaultCurrency;
    public BillStatus Status { get; private set; }
    public string? Notes { get; private set; }
    public string? AttachmentUrl { get; private set; }
    public DateTime CreatedAtUtc { get; private set; } = DateTime.UtcNow;

    public IReadOnlyList<BillLine> Lines => _lines.AsReadOnly();
    public IReadOnlyList<BillPayment> Payments => _payments.AsReadOnly();

    public Money Subtotal => _lines.Aggregate(
        Money.Zero(Currency), (running, line) => running.Add(line.LineNet));

    public Money TaxTotal => _lines.Aggregate(
        Money.Zero(Currency), (running, line) => running.Add(line.LineTax));

    public Money Total => Subtotal.Add(TaxTotal);

    public Money AmountPaid => _payments.Aggregate(
        Money.Zero(Currency), (running, p) => running.Add(p.Amount));

    public Money AmountDue => new(Math.Max(0, Total.Amount - AmountPaid.Amount), Currency);

    /// <summary>True when the bill is past its due date and not yet settled or cancelled.</summary>
    public bool IsOverdue =>
        Status is not (BillStatus.Paid or BillStatus.Cancelled)
        && DueDate < DateOnly.FromDateTime(DateTime.UtcNow);

    private Bill() { }

    public static Bill Create(
        string billNumber,
        string vendorName,
        DateOnly issueDate,
        DateOnly dueDate,
        string currency = Money.DefaultCurrency,
        string? supplierReference = null,
        string? notes = null,
        Guid? vendorId = null)
    {
        if (string.IsNullOrWhiteSpace(billNumber))
            throw new DomainException("Bill number is required.");
        if (string.IsNullOrWhiteSpace(vendorName))
            throw new DomainException("Vendor name is required.");
        if (dueDate < issueDate)
            throw new DomainException("Due date cannot be before the issue date.");

        return new Bill
        {
            BillNumber = billNumber.Trim(),
            VendorName = vendorName.Trim(),
            VendorId = vendorId,
            IssueDate = issueDate,
            DueDate = dueDate,
            Currency = currency.ToUpperInvariant(),
            SupplierReference = Clean(supplierReference),
            Notes = Clean(notes),
            Status = BillStatus.Draft
        };
    }

    public BillLine AddLine(
        string description,
        Guid? expenseCategoryId,
        decimal quantity,
        Money unitCost,
        TaxRate taxRate)
    {
        EnsureEditable();
        if (unitCost.Currency != Currency)
            throw new DomainException("All bill lines must use the bill currency.");

        var line = new BillLine(description, expenseCategoryId, quantity, unitCost, taxRate);
        _lines.Add(line);
        return line;
    }

    public void UpdateDetails(
        string vendorName,
        DateOnly issueDate,
        DateOnly dueDate,
        string? supplierReference,
        string? notes)
    {
        EnsureEditable();
        if (string.IsNullOrWhiteSpace(vendorName))
            throw new DomainException("Vendor name is required.");
        if (dueDate < issueDate)
            throw new DomainException("Due date cannot be before the issue date.");

        VendorName = vendorName.Trim();
        IssueDate = issueDate;
        DueDate = dueDate;
        SupplierReference = Clean(supplierReference);
        Notes = Clean(notes);
    }

    public void ClearLines()
    {
        EnsureEditable();
        _lines.Clear();
    }

    public void SetAttachment(string? url) => AttachmentUrl = Clean(url);

    /// <summary>Approves a draft bill for payment.</summary>
    public void Approve()
    {
        if (Status != BillStatus.Draft)
            throw new DomainException($"Only draft bills can be approved (was {Status}).");
        if (_lines.Count == 0)
            throw new DomainException("Cannot approve a bill with no lines.");
        if (Total.Amount <= 0)
            throw new DomainException("Cannot approve a bill with a zero total.");

        Status = BillStatus.AwaitingPayment;
    }

    public BillPayment RecordPayment(Money amount, DateOnly paidOn, string method, string? reference)
    {
        if (Status is BillStatus.Cancelled)
            throw new DomainException("Cannot pay a cancelled bill.");
        if (Status is BillStatus.Paid)
            throw new DomainException("This bill is already fully paid.");
        if (Status is BillStatus.Draft)
            throw new DomainException("Approve the bill before recording a payment.");
        if (amount.Currency != Currency)
            throw new DomainException("Payment currency must match the bill currency.");
        if (amount.Amount > AmountDue.Amount)
            throw new DomainException("Payment exceeds the amount due.");

        var payment = new BillPayment(amount, paidOn, method, reference);
        _payments.Add(payment);

        Status = AmountDue.Amount <= 0 ? BillStatus.Paid : BillStatus.PartiallyPaid;
        return payment;
    }

    public void Cancel()
    {
        if (Status is BillStatus.Paid)
            throw new DomainException("Cannot cancel a fully paid bill.");
        Status = BillStatus.Cancelled;
    }

    private void EnsureEditable()
    {
        if (Status is not BillStatus.Draft)
            throw new DomainException($"Bill {BillNumber} can no longer be edited ({Status}).");
    }

    private static string? Clean(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
