namespace NikaFitness.Application.Common.Exceptions;

/// <summary>Thrown when a requested resource does not exist. Maps to HTTP 404.</summary>
public sealed class NotFoundException : Exception
{
    public NotFoundException(string name, object key)
        : base($"{name} ({key}) was not found.") { }

    public NotFoundException(string message) : base(message) { }
}
