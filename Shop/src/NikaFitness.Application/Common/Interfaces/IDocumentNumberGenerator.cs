namespace NikaFitness.Application.Common.Interfaces;

/// <summary>
/// Issues sequential, human-friendly document numbers (e.g. INV-00001) backed by a
/// persisted counter. Implementations must advance the counter atomically.
/// </summary>
public interface IDocumentNumberGenerator
{
    /// <summary>
    /// Returns the next formatted number for the sequence identified by
    /// <paramref name="sequenceKey"/>, using <paramref name="prefix"/> for formatting.
    /// </summary>
    Task<string> NextAsync(
        string sequenceKey,
        string prefix,
        CancellationToken cancellationToken = default);
}
