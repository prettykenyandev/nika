namespace NikaFitness.Application.Common.Interfaces;

/// <summary>
/// Abstraction over binary file storage (receipts, logos, document PDFs). The dev
/// implementation writes to local disk; production can swap in a cloud bucket.
/// </summary>
public interface IFileStorage
{
    /// <summary>
    /// Persists <paramref name="content"/> and returns a publicly resolvable URL to it.
    /// </summary>
    Task<string> SaveAsync(
        Stream content,
        string fileName,
        string contentType,
        CancellationToken cancellationToken = default);
}
