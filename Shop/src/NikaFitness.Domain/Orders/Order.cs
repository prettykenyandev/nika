using NikaFitness.Domain.Common;
using NikaFitness.Domain.Orders.Events;
using NikaFitness.Domain.ValueObjects;

namespace NikaFitness.Domain.Orders;

/// <summary>
/// The order aggregate. Owns its line items and is the single place where order
/// state transitions are enforced.
/// </summary>
public sealed class Order : AggregateRoot
{
    private readonly List<OrderItem> _items = new();

    public string OrderNumber { get; private set; } = default!;
    public Guid? CustomerId { get; private set; }
    public string CustomerEmail { get; private set; } = default!;
    public Address ShippingAddress { get; private set; } = default!;
    public OrderStatus Status { get; private set; }
    public string Currency { get; private set; } = Money.DefaultCurrency;
    public DateTime CreatedAtUtc { get; private set; } = DateTime.UtcNow;
    public DateTime? PaidAtUtc { get; private set; }

    public IReadOnlyList<OrderItem> Items => _items.AsReadOnly();

    public Money Total => _items.Aggregate(
        Money.Zero(Currency),
        (running, item) => running.Add(item.LineTotal));

    private Order() { }

    private Order(string orderNumber, string customerEmail, Address shippingAddress, Guid? customerId)
    {
        OrderNumber = orderNumber;
        CustomerEmail = customerEmail;
        ShippingAddress = shippingAddress;
        CustomerId = customerId;
        Status = OrderStatus.PendingPayment;
    }

    public static Order Create(string customerEmail, Address shippingAddress, Guid? customerId = null)
    {
        if (string.IsNullOrWhiteSpace(customerEmail))
            throw new DomainException("Customer email is required.");

        var orderNumber = GenerateOrderNumber();
        var order = new Order(orderNumber, customerEmail.Trim().ToLowerInvariant(), shippingAddress, customerId);
        return order;
    }

    public OrderItem AddItem(
        Guid productId,
        Guid productVariantId,
        string productName,
        string sku,
        Money unitPrice,
        int quantity)
    {
        EnsureEditable();

        if (unitPrice.Currency != Currency)
            throw new DomainException("All order items must share the order currency.");

        var item = new OrderItem(productId, productVariantId, productName, sku, unitPrice, quantity);
        _items.Add(item);
        return item;
    }

    /// <summary>Finalises the order so it can accept payment. Raises a domain event.</summary>
    public void Place()
    {
        if (_items.Count == 0)
            throw new DomainException("Cannot place an order with no items.");
        EnsureEditable();
        Raise(new OrderPlacedEvent(Id, CustomerEmail));
    }

    public void MarkPaid()
    {
        if (Status is OrderStatus.Paid) return;
        if (Status is not OrderStatus.PendingPayment)
            throw new DomainException($"Cannot mark a {Status} order as paid.");

        Status = OrderStatus.Paid;
        PaidAtUtc = DateTime.UtcNow;
        Raise(new OrderPaidEvent(Id));
    }

    public void MarkPaymentFailed(string reason)
    {
        if (Status is not OrderStatus.PendingPayment) return;
        Status = OrderStatus.PaymentFailed;
        Raise(new OrderPaymentFailedEvent(Id, reason));
    }

    public void Cancel()
    {
        if (Status is OrderStatus.Fulfilled)
            throw new DomainException("Cannot cancel a fulfilled order.");
        Status = OrderStatus.Cancelled;
    }

    public void Fulfil()
    {
        if (Status is not OrderStatus.Paid)
            throw new DomainException("Only paid orders can be fulfilled.");
        Status = OrderStatus.Fulfilled;
    }

    private void EnsureEditable()
    {
        if (Status is not OrderStatus.PendingPayment)
            throw new DomainException($"Order {OrderNumber} can no longer be modified ({Status}).");
    }

    private static string GenerateOrderNumber()
        => $"NF-{DateTime.UtcNow:yyMMdd}-{Guid.NewGuid().ToString("N")[..6].ToUpperInvariant()}";
}
