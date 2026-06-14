using NikaFitness.Domain.Common;

namespace NikaFitness.Domain.Sequences;

/// <summary>
/// A persisted, monotonically increasing counter used to mint human-friendly,
/// gap-free document numbers (e.g. INV-00001, BILL-00012). Identified by its
/// <see cref="Key"/> (one row per document type).
/// </summary>
public sealed class NumberSequence : Entity
{
    /// <summary>Stable lookup key for the sequence, e.g. "invoice", "bill", "purchase-order".</summary>
    public string Key { get; private set; } = default!;

    /// <summary>The next value to be issued.</summary>
    public long NextValue { get; private set; } = 1;

    private NumberSequence() { }

    public static NumberSequence Start(string key)
    {
        if (string.IsNullOrWhiteSpace(key))
            throw new DomainException("Sequence key is required.");

        return new NumberSequence { Key = key.Trim().ToLowerInvariant(), NextValue = 1 };
    }

    /// <summary>
    /// Reserves the next value and advances the counter, returning a formatted document
    /// number such as <c>INV-00001</c>.
    /// </summary>
    public string Next(string prefix)
    {
        if (string.IsNullOrWhiteSpace(prefix))
            throw new DomainException("A document prefix is required.");

        var value = NextValue;
        NextValue++;
        return $"{prefix.Trim().ToUpperInvariant()}-{value:D5}";
    }
}
