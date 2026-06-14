using MediatR;
using Microsoft.EntityFrameworkCore;
using NikaFitness.Application.Common.Interfaces;
using NikaFitness.Application.Reporting.Dtos;
using NikaFitness.Domain.Expenses;
using NikaFitness.Domain.Orders;
using NikaFitness.Domain.Purchasing;
using NikaFitness.Domain.Receivables;

namespace NikaFitness.Application.Reporting.Queries;

internal static class ReportConstants
{
    public const int LowStockThreshold = 5;

    public static readonly OrderStatus[] RevenueOrderStatuses =
        { OrderStatus.Paid, OrderStatus.Fulfilled };

    public static readonly InvoiceStatus[] IssuedInvoiceStatuses =
        { InvoiceStatus.Sent, InvoiceStatus.PartiallyPaid, InvoiceStatus.Paid };

    public static readonly BillStatus[] PostedBillStatuses =
        { BillStatus.AwaitingPayment, BillStatus.PartiallyPaid, BillStatus.Paid };

    public static async Task<string> CurrencyAsync(IApplicationDbContext db, CancellationToken ct) =>
        await db.CompanySettings.AsNoTracking().Select(s => s.Currency)
            .FirstOrDefaultAsync(ct) ?? "KES";

    public static DateTime OrderDate(Order o) => o.PaidAtUtc ?? o.CreatedAtUtc;
}

// ---------------------------------------------------------------------------
// Dashboard KPIs
// ---------------------------------------------------------------------------
public sealed record GetDashboardSummaryQuery : IRequest<DashboardSummaryDto>;

public sealed class GetDashboardSummaryQueryHandler(IApplicationDbContext db)
    : IRequestHandler<GetDashboardSummaryQuery, DashboardSummaryDto>
{
    public async Task<DashboardSummaryDto> Handle(
        GetDashboardSummaryQuery request, CancellationToken cancellationToken)
    {
        var currency = await ReportConstants.CurrencyAsync(db, cancellationToken);
        var now = DateTime.UtcNow;
        var monthStart = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc);

        var orders = await db.Orders.AsNoTracking().Include(o => o.Items).ToListAsync(cancellationToken);
        var revenueThisMonth = orders
            .Where(o => ReportConstants.RevenueOrderStatuses.Contains(o.Status)
                        && ReportConstants.OrderDate(o) >= monthStart)
            .Sum(o => o.Total.Amount);

        var bills = await db.Bills.AsNoTracking().Include(b => b.Lines).Include(b => b.Payments)
            .ToListAsync(cancellationToken);
        var monthStartDate = DateOnly.FromDateTime(monthStart);
        var expensesThisMonth = bills
            .Where(b => ReportConstants.PostedBillStatuses.Contains(b.Status)
                        && b.IssueDate >= monthStartDate)
            .Sum(b => b.Total.Amount);

        var outstandingReceivables = 0m;
        var overdueReceivables = 0m;
        var invoices = await db.Invoices.AsNoTracking().Include(i => i.Lines).Include(i => i.Payments)
            .ToListAsync(cancellationToken);
        foreach (var i in invoices.Where(i => i.Status is InvoiceStatus.Sent or InvoiceStatus.PartiallyPaid))
        {
            outstandingReceivables += i.AmountDue.Amount;
            if (i.IsOverdue) overdueReceivables += i.AmountDue.Amount;
        }

        var outstandingPayables = 0m;
        var overduePayables = 0m;
        foreach (var b in bills.Where(b => b.Status is BillStatus.AwaitingPayment or BillStatus.PartiallyPaid))
        {
            outstandingPayables += b.AmountDue.Amount;
            if (b.IsOverdue) overduePayables += b.AmountDue.Amount;
        }

        var products = await db.Products.AsNoTracking().Include(p => p.Variants).ToListAsync(cancellationToken);
        var variants = products.SelectMany(p => p.Variants).ToList();
        var lowStockCount = variants.Count(v => v.StockQuantity <= ReportConstants.LowStockThreshold);
        var inventoryValue = variants.Sum(v => v.Price.Amount * v.StockQuantity);

        var openPurchaseOrders = await db.PurchaseOrders.AsNoTracking()
            .CountAsync(p => p.Status == PurchaseOrderStatus.Sent
                          || p.Status == PurchaseOrderStatus.PartiallyReceived, cancellationToken);

        return new DashboardSummaryDto(
            currency,
            revenueThisMonth,
            expensesThisMonth,
            revenueThisMonth - expensesThisMonth,
            outstandingReceivables,
            overdueReceivables,
            outstandingPayables,
            overduePayables,
            lowStockCount,
            inventoryValue,
            openPurchaseOrders);
    }
}

// ---------------------------------------------------------------------------
// Profit & Loss
// ---------------------------------------------------------------------------
public sealed record GetProfitAndLossQuery(DateOnly? From, DateOnly? To) : IRequest<ProfitAndLossDto>;

public sealed class GetProfitAndLossQueryHandler(IApplicationDbContext db)
    : IRequestHandler<GetProfitAndLossQuery, ProfitAndLossDto>
{
    public async Task<ProfitAndLossDto> Handle(
        GetProfitAndLossQuery request, CancellationToken cancellationToken)
    {
        var (from, to) = Period.Resolve(request.From, request.To);
        var currency = await ReportConstants.CurrencyAsync(db, cancellationToken);
        var fromDt = from.ToDateTime(TimeOnly.MinValue);
        var toDt = to.ToDateTime(TimeOnly.MaxValue);

        var orders = await db.Orders.AsNoTracking().Include(o => o.Items).ToListAsync(cancellationToken);
        var orderRevenue = orders
            .Where(o => ReportConstants.RevenueOrderStatuses.Contains(o.Status)
                        && ReportConstants.OrderDate(o) >= fromDt
                        && ReportConstants.OrderDate(o) <= toDt)
            .Sum(o => o.Total.Amount);

        var invoices = await db.Invoices.AsNoTracking().Include(i => i.Lines).ToListAsync(cancellationToken);
        var invoicedRevenue = invoices
            .Where(i => ReportConstants.IssuedInvoiceStatuses.Contains(i.Status)
                        && i.IssueDate >= from && i.IssueDate <= to)
            .Sum(i => i.Total.Amount);

        var bills = await db.Bills.AsNoTracking().Include(b => b.Lines).ToListAsync(cancellationToken);
        var postedBills = bills
            .Where(b => ReportConstants.PostedBillStatuses.Contains(b.Status)
                        && b.IssueDate >= from && b.IssueDate <= to)
            .ToList();
        var totalExpenses = postedBills.Sum(b => b.Total.Amount);

        var categoryNames = await db.ExpenseCategories.AsNoTracking()
            .ToDictionaryAsync(c => c.Id, c => c.Name, cancellationToken);

        var byCategory = postedBills
            .SelectMany(b => b.Lines)
            .GroupBy(l => l.ExpenseCategoryId)
            .Select(g => new ExpenseBreakdownDto(
                g.Key.HasValue && categoryNames.TryGetValue(g.Key.Value, out var name) ? name : "Uncategorised",
                g.Sum(l => l.LineTotal.Amount)))
            .OrderByDescending(x => x.Amount)
            .ToList();

        var totalRevenue = orderRevenue + invoicedRevenue;

        return new ProfitAndLossDto(
            from, to, currency,
            orderRevenue, invoicedRevenue, totalRevenue,
            totalExpenses, totalRevenue - totalExpenses, byCategory);
    }
}

// ---------------------------------------------------------------------------
// AR / AP aging
// ---------------------------------------------------------------------------
public sealed record GetReceivablesAgingQuery : IRequest<AgingReportDto>;

public sealed class GetReceivablesAgingQueryHandler(IApplicationDbContext db)
    : IRequestHandler<GetReceivablesAgingQuery, AgingReportDto>
{
    public async Task<AgingReportDto> Handle(
        GetReceivablesAgingQuery request, CancellationToken cancellationToken)
    {
        var currency = await ReportConstants.CurrencyAsync(db, cancellationToken);
        var invoices = await db.Invoices.AsNoTracking().Include(i => i.Lines).Include(i => i.Payments)
            .ToListAsync(cancellationToken);

        var open = invoices
            .Where(i => i.Status is InvoiceStatus.Sent or InvoiceStatus.PartiallyPaid)
            .Select(i => (i.DueDate, Amount: i.AmountDue.Amount));

        return Aging.Build(currency, open);
    }
}

public sealed record GetPayablesAgingQuery : IRequest<AgingReportDto>;

public sealed class GetPayablesAgingQueryHandler(IApplicationDbContext db)
    : IRequestHandler<GetPayablesAgingQuery, AgingReportDto>
{
    public async Task<AgingReportDto> Handle(
        GetPayablesAgingQuery request, CancellationToken cancellationToken)
    {
        var currency = await ReportConstants.CurrencyAsync(db, cancellationToken);
        var bills = await db.Bills.AsNoTracking().Include(b => b.Lines).Include(b => b.Payments)
            .ToListAsync(cancellationToken);

        var open = bills
            .Where(b => b.Status is BillStatus.AwaitingPayment or BillStatus.PartiallyPaid)
            .Select(b => (b.DueDate, Amount: b.AmountDue.Amount));

        return Aging.Build(currency, open);
    }
}

internal static class Aging
{
    public static AgingReportDto Build(string currency, IEnumerable<(DateOnly DueDate, decimal Amount)> items)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var buckets = new (string Label, decimal Amount, int Count)[]
        {
            ("Current", 0m, 0),
            ("1-30 days", 0m, 0),
            ("31-60 days", 0m, 0),
            ("61-90 days", 0m, 0),
            ("90+ days", 0m, 0)
        };

        decimal total = 0;
        foreach (var (dueDate, amount) in items)
        {
            if (amount <= 0) continue;
            total += amount;
            var daysOverdue = today.DayNumber - dueDate.DayNumber;
            var index = daysOverdue switch
            {
                <= 0 => 0,
                <= 30 => 1,
                <= 60 => 2,
                <= 90 => 3,
                _ => 4
            };
            buckets[index].Amount += amount;
            buckets[index].Count += 1;
        }

        return new AgingReportDto(
            currency, total,
            buckets.Select(b => new AgingBucketDto(b.Label, b.Amount, b.Count)).ToList());
    }
}

// ---------------------------------------------------------------------------
// Sales analytics
// ---------------------------------------------------------------------------
public sealed record GetSalesAnalyticsQuery(DateOnly? From, DateOnly? To) : IRequest<SalesAnalyticsDto>;

public sealed class GetSalesAnalyticsQueryHandler(IApplicationDbContext db)
    : IRequestHandler<GetSalesAnalyticsQuery, SalesAnalyticsDto>
{
    public async Task<SalesAnalyticsDto> Handle(
        GetSalesAnalyticsQuery request, CancellationToken cancellationToken)
    {
        var (from, to) = Period.Resolve(request.From, request.To);
        var currency = await ReportConstants.CurrencyAsync(db, cancellationToken);
        var fromDt = from.ToDateTime(TimeOnly.MinValue);
        var toDt = to.ToDateTime(TimeOnly.MaxValue);

        var orders = (await db.Orders.AsNoTracking().Include(o => o.Items).ToListAsync(cancellationToken))
            .Where(o => ReportConstants.RevenueOrderStatuses.Contains(o.Status)
                        && ReportConstants.OrderDate(o) >= fromDt
                        && ReportConstants.OrderDate(o) <= toDt)
            .ToList();

        var totalSales = orders.Sum(o => o.Total.Amount);

        var topProducts = orders
            .SelectMany(o => o.Items)
            .GroupBy(i => new { i.ProductId, i.ProductName })
            .Select(g => new TopProductDto(
                g.Key.ProductId,
                g.Key.ProductName,
                g.Sum(i => i.Quantity),
                g.Sum(i => i.LineTotal.Amount)))
            .OrderByDescending(p => p.Revenue)
            .Take(10)
            .ToList();

        var monthlyTrend = orders
            .GroupBy(o => new DateOnly(ReportConstants.OrderDate(o).Year, ReportConstants.OrderDate(o).Month, 1))
            .OrderBy(g => g.Key)
            .Select(g => new SalesTrendPointDto(
                g.Key.ToString("yyyy-MM"),
                g.Sum(o => o.Total.Amount),
                g.Count()))
            .ToList();

        return new SalesAnalyticsDto(from, to, currency, totalSales, orders.Count, topProducts, monthlyTrend);
    }
}

// ---------------------------------------------------------------------------
// Inventory valuation & low stock
// ---------------------------------------------------------------------------
public sealed record GetInventoryValuationQuery : IRequest<InventoryValuationDto>;

public sealed class GetInventoryValuationQueryHandler(IApplicationDbContext db)
    : IRequestHandler<GetInventoryValuationQuery, InventoryValuationDto>
{
    public async Task<InventoryValuationDto> Handle(
        GetInventoryValuationQuery request, CancellationToken cancellationToken)
    {
        var currency = await ReportConstants.CurrencyAsync(db, cancellationToken);
        var products = await db.Products.AsNoTracking().Include(p => p.Variants).ToListAsync(cancellationToken);

        var variants = products
            .SelectMany(p => p.Variants.Select(v => (Product: p, Variant: v)))
            .ToList();

        var totalValue = variants.Sum(x => x.Variant.Price.Amount * x.Variant.StockQuantity);
        var totalUnits = variants.Sum(x => x.Variant.StockQuantity);

        var lowStock = variants
            .Where(x => x.Variant.StockQuantity <= ReportConstants.LowStockThreshold)
            .OrderBy(x => x.Variant.StockQuantity)
            .Select(x => new LowStockItemDto(
                x.Product.Id,
                x.Product.Name,
                x.Variant.Id,
                x.Variant.Sku,
                x.Variant.Name,
                x.Variant.StockQuantity))
            .ToList();

        return new InventoryValuationDto(
            currency, totalValue, variants.Count, totalUnits,
            ReportConstants.LowStockThreshold, lowStock);
    }
}

// ---------------------------------------------------------------------------
// VAT summary
// ---------------------------------------------------------------------------
public sealed record GetVatSummaryQuery(DateOnly? From, DateOnly? To) : IRequest<VatSummaryDto>;

public sealed class GetVatSummaryQueryHandler(IApplicationDbContext db)
    : IRequestHandler<GetVatSummaryQuery, VatSummaryDto>
{
    public async Task<VatSummaryDto> Handle(
        GetVatSummaryQuery request, CancellationToken cancellationToken)
    {
        var (from, to) = Period.Resolve(request.From, request.To);
        var currency = await ReportConstants.CurrencyAsync(db, cancellationToken);

        var invoices = await db.Invoices.AsNoTracking().Include(i => i.Lines).ToListAsync(cancellationToken);
        var outputTax = invoices
            .Where(i => ReportConstants.IssuedInvoiceStatuses.Contains(i.Status)
                        && i.IssueDate >= from && i.IssueDate <= to)
            .Sum(i => i.TaxTotal.Amount);

        var bills = await db.Bills.AsNoTracking().Include(b => b.Lines).ToListAsync(cancellationToken);
        var inputTax = bills
            .Where(b => ReportConstants.PostedBillStatuses.Contains(b.Status)
                        && b.IssueDate >= from && b.IssueDate <= to)
            .Sum(b => b.TaxTotal.Amount);

        return new VatSummaryDto(from, to, currency, outputTax, inputTax, outputTax - inputTax);
    }
}

internal static class Period
{
    /// <summary>Defaults to the current calendar month when no bounds are supplied.</summary>
    public static (DateOnly From, DateOnly To) Resolve(DateOnly? from, DateOnly? to)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var resolvedFrom = from ?? new DateOnly(today.Year, today.Month, 1);
        var resolvedTo = to ?? today;
        if (resolvedTo < resolvedFrom) (resolvedFrom, resolvedTo) = (resolvedTo, resolvedFrom);
        return (resolvedFrom, resolvedTo);
    }
}
