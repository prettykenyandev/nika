using MediatR;
using NikaFitness.Application.Auditing.Queries;

namespace NikaFitness.Api.Endpoints;

/// <summary>Audit-log viewer endpoints under <c>/api/admin/audit-log</c>.</summary>
public static class AuditEndpoints
{
    public static IEndpointRouteBuilder MapAuditEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/admin")
            .WithTags("Audit")
            .RequireAuthorization(policy => policy.RequireRole(AuthEndpoints.AdminRole));

        group.MapGet("/audit-log", async (
            ISender sender, string? search, int page = 1, int pageSize = 50) =>
            Results.Ok(await sender.Send(new GetAuditLogQuery(search, page, pageSize))));

        return app;
    }
}
