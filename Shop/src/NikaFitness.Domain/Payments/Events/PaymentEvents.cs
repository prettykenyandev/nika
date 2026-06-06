using NikaFitness.Domain.Common;

namespace NikaFitness.Domain.Payments.Events;

public sealed record PaymentSucceededEvent(Guid PaymentId, Guid OrderId, string ProviderReference) : IDomainEvent;

public sealed record PaymentFailedEvent(Guid PaymentId, Guid OrderId, string Reason) : IDomainEvent;
