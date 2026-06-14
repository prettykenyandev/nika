using Microsoft.EntityFrameworkCore;
using NikaFitness.Application.Common.Interfaces;
using NikaFitness.Domain.Sequences;
using NikaFitness.Infrastructure.Persistence;

namespace NikaFitness.Infrastructure.Sequences;

/// <summary>
/// Generates sequential document numbers backed by the <see cref="NumberSequence"/>
/// table. Admin writes are low-concurrency, so a load-increment-save is sufficient and
/// the unique key index guards against duplicate sequence rows.
/// </summary>
public sealed class DocumentNumberGenerator(ApplicationDbContext db) : IDocumentNumberGenerator
{
    public async Task<string> NextAsync(
        string sequenceKey,
        string prefix,
        CancellationToken cancellationToken = default)
    {
        var key = sequenceKey.Trim().ToLowerInvariant();

        var sequence = await db.NumberSequences
            .FirstOrDefaultAsync(s => s.Key == key, cancellationToken);

        if (sequence is null)
        {
            sequence = NumberSequence.Start(key);
            db.NumberSequences.Add(sequence);
        }

        var number = sequence.Next(prefix);
        await db.SaveChangesAsync(cancellationToken);
        return number;
    }
}
