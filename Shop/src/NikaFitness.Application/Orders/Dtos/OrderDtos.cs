using NikaFitness.Domain.Orders;

namespace NikaFitness.Application.Orders.Dtos;

public sealed record OrderItemDto(
    string ProductName,
    string Sku,
    decimal UnitPrice,
    int Quantity,
    decimal LineTotal);

public sealed record OrderDto(
    Guid Id,
    string OrderNumber,
    string CustomerEmail,
    OrderStatus Status,
    decimal Total,
    string Currency,
    DateTime CreatedAtUtc,
    DateTime? PaidAtUtc,
    IReadOnlyList<OrderItemDto> Items);

public sealed record CheckoutResult(
    Guid OrderId,
    string OrderNumber,
    Guid PaymentId,
    bool PaymentInitiated,
    string Message);
