using NikaFitness.Domain.Common;

namespace NikaFitness.Domain.Auditing;

/// <summary>
/// A lightweight, append-only record of a state-changing action taken in the system —
/// who did what and when. Used to power the admin audit-log viewer.
/// </summary>
public sealed class AuditLogEntry : AggregateRoot
{
    public DateTime OccurredAtUtc { get; private set; }
    public Guid? ActorId { get; private set; }
    public string ActorEmail { get; private set; } = default!;

    /// <summary>The command/action name, e.g. <c>CreateInvoiceCommand</c>.</summary>
    public string Action { get; private set; } = default!;
    public string? Summary { get; private set; }

    private AuditLogEntry() { }

    public static AuditLogEntry Record(
        Guid? actorId,
        string? actorEmail,
        string action,
        string? summary = null)
    {
        if (string.IsNullOrWhiteSpace(action))
            throw new DomainException("Audit action is required.");

        return new AuditLogEntry
        {
            OccurredAtUtc = DateTime.UtcNow,
            ActorId = actorId,
            ActorEmail = string.IsNullOrWhiteSpace(actorEmail) ? "system" : actorEmail.Trim(),
            Action = action.Trim(),
            Summary = string.IsNullOrWhiteSpace(summary) ? null : summary.Trim()
        };
    }
}
