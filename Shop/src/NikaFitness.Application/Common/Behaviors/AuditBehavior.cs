using MediatR;
using NikaFitness.Application.Common.Interfaces;
using NikaFitness.Domain.Auditing;

namespace NikaFitness.Application.Common.Behaviors;

/// <summary>
/// Records an audit-log entry for every successfully handled command (any request type whose
/// name ends in <c>Command</c>). Queries are ignored. Failures are not recorded because the
/// entry is written only after the inner handler completes.
/// </summary>
public sealed class AuditBehavior<TRequest, TResponse>(
    IApplicationDbContext db,
    ICurrentUser currentUser)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        var response = await next();

        var name = typeof(TRequest).Name;
        if (name.EndsWith("Command", StringComparison.Ordinal))
        {
            var entry = AuditLogEntry.Record(
                currentUser.UserId,
                currentUser.Email,
                name);

            db.AuditLog.Add(entry);
            await db.SaveChangesAsync(cancellationToken);
        }

        return response;
    }
}
