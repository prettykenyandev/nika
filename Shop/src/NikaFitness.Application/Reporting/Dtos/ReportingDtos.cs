namespace NikaFitness.Application.Reporting.Dtos;

/// <summary>Headline KPIs for the admin home dashboard.</summary>
public sealed record DashboardSummaryDto(
    string Currency,
    decimal RevenueThisMonth,
    decimal ExpensesThisMonth,
    decimal NetThisMonth,
    decimal OutstandingReceivables,
    decimal OverdueReceivables,
    decimal OutstandingPayables,
    decimal OverduePayables,
    int LowStockCount,
    decimal InventoryValue,
    int OpenPurchaseOrders);

public sealed record ExpenseBreakdownDto(string Category, decimal Amount);

/// <summary>Accrual-basis profit &amp; loss over a period.</summary>
public sealed record ProfitAndLossDto(
    DateOnly From,
    DateOnly To,
    string Currency,
    decimal OrderRevenue,
    decimal InvoicedRevenue,
    decimal TotalRevenue,
    decimal TotalExpenses,
    decimal NetProfit,
    IReadOnlyList<ExpenseBreakdownDto> ExpensesByCategory);

public sealed record AgingBucketDto(
    string Label,
    decimal Amount,
    int Count);

public sealed record AgingReportDto(
    string Currency,
    decimal Total,
    IReadOnlyList<AgingBucketDto> Buckets);

public sealed record TopProductDto(
    Guid ProductId,
    string ProductName,
    int QuantitySold,
    decimal Revenue);

public sealed record SalesTrendPointDto(
    string Period,
    decimal Revenue,
    int OrderCount);

public sealed record SalesAnalyticsDto(
    DateOnly From,
    DateOnly To,
    string Currency,
    decimal TotalSales,
    int OrderCount,
    IReadOnlyList<TopProductDto> TopProducts,
    IReadOnlyList<SalesTrendPointDto> MonthlyTrend);

public sealed record LowStockItemDto(
    Guid ProductId,
    string ProductName,
    Guid VariantId,
    string Sku,
    string VariantName,
    int StockQuantity);

public sealed record InventoryValuationDto(
    string Currency,
    decimal TotalValue,
    int VariantCount,
    int TotalUnits,
    int LowStockThreshold,
    IReadOnlyList<LowStockItemDto> LowStock);

public sealed record VatSummaryDto(
    DateOnly From,
    DateOnly To,
    string Currency,
    decimal OutputTax,
    decimal InputTax,
    decimal NetVatDue);
