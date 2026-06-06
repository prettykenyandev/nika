using System.Text.RegularExpressions;
using NikaFitness.Domain.Common;

namespace NikaFitness.Domain.ValueObjects;

/// <summary>
/// A URL-friendly identifier for catalog items (e.g. "mens-training-tee").
/// </summary>
public sealed partial record Slug
{
    public string Value { get; }

    private Slug(string value) => Value = value;

    public static Slug Create(string input)
    {
        if (string.IsNullOrWhiteSpace(input))
            throw new DomainException("Slug source text is required.");

        var slug = NonAlphanumeric().Replace(input.Trim().ToLowerInvariant(), "-")
            .Trim('-');

        if (slug.Length == 0)
            throw new DomainException("Slug could not be generated from the supplied text.");

        return new Slug(slug);
    }

    public static Slug FromExisting(string value) => new(value);

    public override string ToString() => Value;

    [GeneratedRegex("[^a-z0-9]+")]
    private static partial Regex NonAlphanumeric();
}
