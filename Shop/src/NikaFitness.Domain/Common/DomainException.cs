namespace NikaFitness.Domain.Common;

/// <summary>
/// Thrown when an operation would violate a domain invariant.
/// </summary>
public sealed class DomainException : Exception
{
    public DomainException(string message) : base(message) { }
}
