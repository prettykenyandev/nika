using Microsoft.AspNetCore.Identity;

namespace NikaFitness.Infrastructure.Identity;

/// <summary>Application user backed by ASP.NET Core Identity, keyed by Guid.</summary>
public sealed class ApplicationUser : IdentityUser<Guid>
{
    public string FullName { get; set; } = string.Empty;
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}
