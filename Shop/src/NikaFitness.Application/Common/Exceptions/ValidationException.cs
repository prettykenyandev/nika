namespace NikaFitness.Application.Common.Exceptions;

/// <summary>Aggregates FluentValidation failures. Maps to HTTP 400 with field errors.</summary>
public sealed class ValidationException : Exception
{
    public IReadOnlyDictionary<string, string[]> Errors { get; }

    public ValidationException(IReadOnlyDictionary<string, string[]> errors)
        : base("One or more validation errors occurred.")
    {
        Errors = errors;
    }
}
