using System.ComponentModel.DataAnnotations;

namespace NikaFitness.Api.Common;

public sealed class JwtOptions
{
    public const string SectionName = "Jwt";

    [Required] public string Issuer { get; set; } = "NikaFitness";
    [Required] public string Audience { get; set; } = "NikaFitness.Storefront";

    /// <summary>Symmetric signing key. Override via configuration/secret in production.</summary>
    [Required, MinLength(32)]
    public string SigningKey { get; set; } = "dev-only-super-secret-signing-key-change-me!";

    public int ExpiryMinutes { get; set; } = 120;
}
