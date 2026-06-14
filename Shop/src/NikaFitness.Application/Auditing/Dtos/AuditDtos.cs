namespace NikaFitness.Application.Auditing.Dtos;

public sealed record AuditLogEntryDto(
    Guid Id,
    DateTime OccurredAtUtc,
    Guid? ActorId,
    string ActorEmail,
    string Action,
    string? Summary);
