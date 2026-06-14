using NikaFitness.Application.Expenses.Dtos;
using NikaFitness.Domain.Expenses;

namespace NikaFitness.Application.Expenses.Queries;

/// <summary>Maps bill aggregates to their read DTOs (computed totals live on the domain).</summary>
internal static class ExpenseMapping
{
    public static BillSummaryDto ToSummary(Bill bill) => new(
        bill.Id,
        bill.BillNumber,
        bill.VendorName,
        bill.IssueDate,
        bill.DueDate,
        bill.Currency,
        bill.Status.ToString(),
        bill.IsOverdue,
        bill.Total.Amount,
        bill.AmountPaid.Amount,
        bill.AmountDue.Amount);

    public static BillDetailDto ToDetail(Bill bill, IReadOnlyDictionary<Guid, string> categoryNames)
    {
        var lines = bill.Lines.Select(l => new BillLineDto(
            l.Id,
            l.Description,
            l.ExpenseCategoryId,
            l.ExpenseCategoryId.HasValue && categoryNames.TryGetValue(l.ExpenseCategoryId.Value, out var name)
                ? name
                : null,
            l.Quantity,
            l.UnitCost.Amount,
            l.TaxRate.Percent,
            l.LineNet.Amount,
            l.LineTax.Amount,
            l.LineTotal.Amount)).ToList();

        var payments = bill.Payments
            .OrderByDescending(p => p.PaidOn)
            .Select(p => new BillPaymentDto(p.Id, p.Amount.Amount, p.PaidOn, p.Method, p.Reference))
            .ToList();

        return new BillDetailDto(
            bill.Id,
            bill.BillNumber,
            bill.VendorName,
            bill.VendorId,
            bill.SupplierReference,
            bill.IssueDate,
            bill.DueDate,
            bill.Currency,
            bill.Status.ToString(),
            bill.IsOverdue,
            bill.Notes,
            bill.AttachmentUrl,
            bill.Subtotal.Amount,
            bill.TaxTotal.Amount,
            bill.Total.Amount,
            bill.AmountPaid.Amount,
            bill.AmountDue.Amount,
            lines,
            payments);
    }
}
