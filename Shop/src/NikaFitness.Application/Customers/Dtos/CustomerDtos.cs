namespace NikaFitness.Application.Customers.Dtos;

public sealed record CustomerSummaryDto(
    Guid Id,
    string Name,
    string? Email,
    string? Phone,
    string? City,
    bool IsActive,
    int OrderCount,
    decimal OutstandingBalance,
    string Currency);

public sealed record CustomerOrderDto(
    Guid Id,
    string OrderNumber,
    DateTime CreatedAtUtc,
    string Status,
    decimal Total,
    string Currency);

public sealed record CustomerInvoiceDto(
    Guid Id,
    string InvoiceNumber,
    DateOnly IssueDate,
    DateOnly DueDate,
    string Status,
    bool IsOverdue,
    decimal Total,
    decimal AmountDue,
    string Currency);

public sealed record CustomerDetailDto(
    Guid Id,
    string Name,
    string? Email,
    string? Phone,
    string? AddressLine1,
    string? City,
    string? Country,
    string? Notes,
    bool IsActive,
    string Currency,
    decimal TotalInvoiced,
    decimal OutstandingBalance,
    decimal LifetimeOrderValue,
    IReadOnlyList<CustomerOrderDto> Orders,
    IReadOnlyList<CustomerInvoiceDto> Invoices);

public sealed record CustomerInput(
    string Name,
    string? Email,
    string? Phone,
    string? AddressLine1,
    string? City,
    string? Country,
    string? Notes);
