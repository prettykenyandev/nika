namespace NikaFitness.Domain.Common;

/// <summary>
/// A side effect-free fact that something happened in the domain.
/// </summary>
public interface IDomainEvent
{
    DateTime OccurredOnUtc => DateTime.UtcNow;
}
