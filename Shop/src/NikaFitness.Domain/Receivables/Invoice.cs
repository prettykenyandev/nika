using NikaFitness.Domain.Common;
using NikaFitness.Domain.ValueObjects;

namespace NikaFitness.Domain.Receivables;

/// <summary>
/// The invoice aggregate (accounts receivable). Captures an amount owed by a customer,
/// its line items and any receipts recorded against it. It is the single place where AR
/// state transitions and balance calculations are enforced.
/// </summary>
public sealed class Invoice : AggregateRoot
{
    private readonly List<InvoiceLine> _lines = new();
    private readonly List<InvoicePayment> _payments = new();

    public string InvoiceNumber { get; private set; } = default!;
    public string CustomerName { get; private set; } = default!;
    public Guid? CustomerId { get; private set; }
    public string? CustomerEmail { get; private set; }

    /// <summary>The order this invoice was generated from, if any.</summary>
    public Guid? OrderId { get; private set; }

    public DateOnly IssueDate { get; private set; }
    public DateOnly DueDate { get; private set; }
    public string Currency { get; private set; } = Money.DefaultCurrency;
    public InvoiceStatus Status { get; private set; }
    public string? Notes { get; private set; }
    public DateTime CreatedAtUtc { get; private set; } = DateTime.UtcNow;
    public DateTime? SentAtUtc { get; private set; }

    public IReadOnlyList<InvoiceLine> Lines => _lines.AsReadOnly();
    public IReadOnlyList<InvoicePayment> Payments => _payments.AsReadOnly();

    public Money Subtotal => _lines.Aggregate(
        Money.Zero(Currency), (running, line) => running.Add(line.LineNet));

    public Money TaxTotal => _lines.Aggregate(
        Money.Zero(Currency), (running, line) => running.Add(line.LineTax));

    public Money Total => Subtotal.Add(TaxTotal);

    public Money AmountPaid => _payments.Aggregate(
        Money.Zero(Currency), (running, p) => running.Add(p.Amount));

    public Money AmountDue => new(Math.Max(0, Total.Amount - AmountPaid.Amount), Currency);

    /// <summary>True when the invoice is past its due date and not yet settled or voided.</summary>
    public bool IsOverdue =>
        Status is not (InvoiceStatus.Paid or InvoiceStatus.Void or InvoiceStatus.Draft)
        && DueDate < DateOnly.FromDateTime(DateTime.UtcNow);

    private Invoice() { }

    public static Invoice Create(
        string invoiceNumber,
        string customerName,
        DateOnly issueDate,
        DateOnly dueDate,
        string currency = Money.DefaultCurrency,
        string? customerEmail = null,
        string? notes = null,
        Guid? customerId = null,
        Guid? orderId = null)
    {
        if (string.IsNullOrWhiteSpace(invoiceNumber))
            throw new DomainException("Invoice number is required.");
        if (string.IsNullOrWhiteSpace(customerName))
            throw new DomainException("Customer name is required.");
        if (dueDate < issueDate)
            throw new DomainException("Due date cannot be before the issue date.");

        return new Invoice
        {
            InvoiceNumber = invoiceNumber.Trim(),
            CustomerName = customerName.Trim(),
            CustomerId = customerId,
            CustomerEmail = Clean(customerEmail),
            OrderId = orderId,
            IssueDate = issueDate,
            DueDate = dueDate,
            Currency = currency.ToUpperInvariant(),
            Notes = Clean(notes),
            Status = InvoiceStatus.Draft
        };
    }

    public InvoiceLine AddLine(
        string description,
        decimal quantity,
        Money unitPrice,
        TaxRate taxRate)
    {
        EnsureEditable();
        if (unitPrice.Currency != Currency)
            throw new DomainException("All invoice lines must use the invoice currency.");

        var line = new InvoiceLine(description, quantity, unitPrice, taxRate);
        _lines.Add(line);
        return line;
    }

    public void UpdateDetails(
        string customerName,
        string? customerEmail,
        DateOnly issueDate,
        DateOnly dueDate,
        string? notes)
    {
        EnsureEditable();
        if (string.IsNullOrWhiteSpace(customerName))
            throw new DomainException("Customer name is required.");
        if (dueDate < issueDate)
            throw new DomainException("Due date cannot be before the issue date.");

        CustomerName = customerName.Trim();
        CustomerEmail = Clean(customerEmail);
        IssueDate = issueDate;
        DueDate = dueDate;
        Notes = Clean(notes);
    }

    public void ClearLines()
    {
        EnsureEditable();
        _lines.Clear();
    }

    /// <summary>Issues a draft invoice to the customer.</summary>
    public void Send()
    {
        if (Status != InvoiceStatus.Draft)
            throw new DomainException($"Only draft invoices can be sent (was {Status}).");
        if (_lines.Count == 0)
            throw new DomainException("Cannot send an invoice with no lines.");
        if (Total.Amount <= 0)
            throw new DomainException("Cannot send an invoice with a zero total.");

        Status = InvoiceStatus.Sent;
        SentAtUtc = DateTime.UtcNow;
    }

    public InvoicePayment RecordPayment(Money amount, DateOnly receivedOn, string method, string? reference)
    {
        if (Status is InvoiceStatus.Void)
            throw new DomainException("Cannot record a receipt against a voided invoice.");
        if (Status is InvoiceStatus.Paid)
            throw new DomainException("This invoice is already fully paid.");
        if (Status is InvoiceStatus.Draft)
            throw new DomainException("Send the invoice before recording a receipt.");
        if (amount.Currency != Currency)
            throw new DomainException("Receipt currency must match the invoice currency.");
        if (amount.Amount > AmountDue.Amount)
            throw new DomainException("Receipt exceeds the amount due.");

        var payment = new InvoicePayment(amount, receivedOn, method, reference);
        _payments.Add(payment);

        Status = AmountDue.Amount <= 0 ? InvoiceStatus.Paid : InvoiceStatus.PartiallyPaid;
        return payment;
    }

    public void Void()
    {
        if (Status is InvoiceStatus.Paid)
            throw new DomainException("Cannot void a fully paid invoice.");
        Status = InvoiceStatus.Void;
    }

    private void EnsureEditable()
    {
        if (Status is not InvoiceStatus.Draft)
            throw new DomainException($"Invoice {InvoiceNumber} can no longer be edited ({Status}).");
    }

    private static string? Clean(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
