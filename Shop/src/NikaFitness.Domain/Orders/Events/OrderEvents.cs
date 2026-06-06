using NikaFitness.Domain.Common;

namespace NikaFitness.Domain.Orders.Events;

public sealed record OrderPlacedEvent(Guid OrderId, string CustomerEmail) : IDomainEvent;

public sealed record OrderPaidEvent(Guid OrderId) : IDomainEvent;

public sealed record OrderPaymentFailedEvent(Guid OrderId, string Reason) : IDomainEvent;
