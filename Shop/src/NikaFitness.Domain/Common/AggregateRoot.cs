namespace NikaFitness.Domain.Common;

/// <summary>
/// Marker for an aggregate root. Aggregate roots are the only entities that
/// repositories load and persist directly, and they guard their own invariants.
/// </summary>
public abstract class AggregateRoot : Entity
{
    private readonly List<IDomainEvent> _domainEvents = new();

    public IReadOnlyCollection<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();

    protected void Raise(IDomainEvent domainEvent) => _domainEvents.Add(domainEvent);

    public void ClearDomainEvents() => _domainEvents.Clear();
}
