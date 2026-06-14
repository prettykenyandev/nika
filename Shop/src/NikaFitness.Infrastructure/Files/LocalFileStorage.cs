using Microsoft.Extensions.Configuration;
using NikaFitness.Application.Common.Interfaces;

namespace NikaFitness.Infrastructure.Files;

/// <summary>
/// Stores uploaded files on local disk under the web root's <c>uploads</c> folder and
/// returns an absolute URL served by the API's static-file middleware. Production can
/// replace this with a cloud-bucket implementation of <see cref="IFileStorage"/>.
/// </summary>
public sealed class LocalFileStorage : IFileStorage
{
    private readonly string _rootPath;
    private readonly string _publicBaseUrl;

    public LocalFileStorage(IConfiguration configuration)
    {
        // Defaults work for `dotnet run`; override via Files:RootPath / Files:PublicBaseUrl.
        _rootPath = configuration["Files:RootPath"]
            ?? Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads");
        _publicBaseUrl = (configuration["Files:PublicBaseUrl"] ?? "http://localhost:5087")
            .TrimEnd('/');

        Directory.CreateDirectory(_rootPath);
    }

    public async Task<string> SaveAsync(
        Stream content,
        string fileName,
        string contentType,
        CancellationToken cancellationToken = default)
    {
        var extension = Path.GetExtension(fileName);
        var safeName = $"{Guid.NewGuid():N}{extension}";
        var fullPath = Path.Combine(_rootPath, safeName);

        await using (var file = File.Create(fullPath))
        {
            await content.CopyToAsync(file, cancellationToken);
        }

        return $"{_publicBaseUrl}/uploads/{safeName}";
    }
}
