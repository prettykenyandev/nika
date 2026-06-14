using MediatR;
using Microsoft.EntityFrameworkCore;
using NikaFitness.Application.Auditing.Dtos;
using NikaFitness.Application.Common.Interfaces;
using NikaFitness.Application.Common.Models;

namespace NikaFitness.Application.Auditing.Queries;

public sealed record GetAuditLogQuery(
    string? Search = null,
    int Page = 1,
    int PageSize = 50) : IRequest<PagedResult<AuditLogEntryDto>>;

public sealed class GetAuditLogQueryHandler(IApplicationDbContext db)
    : IRequestHandler<GetAuditLogQuery, PagedResult<AuditLogEntryDto>>
{
    public async Task<PagedResult<AuditLogEntryDto>> Handle(
        GetAuditLogQuery request,
        CancellationToken cancellationToken)
    {
        var page = request.Page < 1 ? 1 : request.Page;
        var pageSize = request.PageSize is < 1 or > 200 ? 50 : request.PageSize;

        var query = db.AuditLog.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var term = request.Search.Trim().ToLower();
            query = query.Where(a =>
                a.Action.ToLower().Contains(term)
                || a.ActorEmail.ToLower().Contains(term));
        }

        var totalCount = await query.CountAsync(cancellationToken);

        var entries = await query
            .OrderByDescending(a => a.OccurredAtUtc)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(a => new AuditLogEntryDto(
                a.Id, a.OccurredAtUtc, a.ActorId, a.ActorEmail, a.Action, a.Summary))
            .ToListAsync(cancellationToken);

        return new PagedResult<AuditLogEntryDto>(entries, page, pageSize, totalCount);
    }
}
