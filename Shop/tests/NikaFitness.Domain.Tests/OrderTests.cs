using NikaFitness.Domain.Common;
using NikaFitness.Domain.Orders;
using NikaFitness.Domain.Orders.Events;
using NikaFitness.Domain.ValueObjects;

namespace NikaFitness.Domain.Tests;

public class OrderTests
{
    private static Address Address() => new(
        "Jane Doe", "123 Ngong Rd", "Nairobi", "Kenya", "254712345678");

    private static Order NewOrder() => Order.Create("Jane@Example.com ", Address());

    [Fact]
    public void Create_Normalises_Email_And_Starts_Pending()
    {
        var order = NewOrder();

        Assert.Equal("jane@example.com", order.CustomerEmail);
        Assert.Equal(OrderStatus.PendingPayment, order.Status);
        Assert.StartsWith("NF-", order.OrderNumber);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_Rejects_Blank_Email(string email)
    {
        Assert.Throws<DomainException>(() => Order.Create(email, Address()));
    }

    [Fact]
    public void Total_Sums_Line_Totals()
    {
        var order = NewOrder();
        order.AddItem(Guid.NewGuid(), Guid.NewGuid(), "Tee", "TEE-M", new Money(1000m), 2);
        order.AddItem(Guid.NewGuid(), Guid.NewGuid(), "Cap", "CAP-1", new Money(500m), 1);

        Assert.Equal(2500m, order.Total.Amount);
        Assert.Equal("KES", order.Total.Currency);
    }

    [Fact]
    public void AddItem_Rejects_Currency_Mismatch()
    {
        var order = NewOrder();
        Assert.Throws<DomainException>(
            () => order.AddItem(Guid.NewGuid(), Guid.NewGuid(), "Tee", "TEE-M", new Money(10m, "USD"), 1));
    }

    [Fact]
    public void Place_Requires_Items_And_Raises_Event()
    {
        var order = NewOrder();
        Assert.Throws<DomainException>(order.Place);

        order.AddItem(Guid.NewGuid(), Guid.NewGuid(), "Tee", "TEE-M", new Money(1000m), 1);
        order.Place();

        Assert.Contains(order.DomainEvents, e => e is OrderPlacedEvent);
    }

    [Fact]
    public void MarkPaid_Transitions_Once_And_Raises_Event()
    {
        var order = NewOrder();
        order.MarkPaid();

        Assert.Equal(OrderStatus.Paid, order.Status);
        Assert.NotNull(order.PaidAtUtc);
        Assert.Contains(order.DomainEvents, e => e is OrderPaidEvent);

        // Idempotent second call is a no-op (no throw).
        order.MarkPaid();
        Assert.Equal(OrderStatus.Paid, order.Status);
    }

    [Fact]
    public void MarkPaid_Rejected_After_Cancellation()
    {
        var order = NewOrder();
        order.Cancel();
        Assert.Throws<DomainException>(order.MarkPaid);
    }

    [Fact]
    public void MarkPaymentFailed_Only_Applies_While_Pending()
    {
        var order = NewOrder();
        order.MarkPaymentFailed("card declined");
        Assert.Equal(OrderStatus.PaymentFailed, order.Status);
    }

    [Fact]
    public void Fulfil_Requires_Paid()
    {
        var order = NewOrder();
        Assert.Throws<DomainException>(order.Fulfil);

        order.MarkPaid();
        order.Fulfil();
        Assert.Equal(OrderStatus.Fulfilled, order.Status);
    }

    [Fact]
    public void Cancel_Rejected_After_Fulfilment()
    {
        var order = NewOrder();
        order.MarkPaid();
        order.Fulfil();
        Assert.Throws<DomainException>(order.Cancel);
    }

    [Fact]
    public void AddItem_Rejected_Once_No_Longer_Pending()
    {
        var order = NewOrder();
        order.MarkPaid();
        Assert.Throws<DomainException>(
            () => order.AddItem(Guid.NewGuid(), Guid.NewGuid(), "Tee", "TEE-M", new Money(1000m), 1));
    }
}
