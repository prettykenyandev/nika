using MediatR;
using Microsoft.EntityFrameworkCore;
using NikaFitness.Application.Common.Exceptions;
using NikaFitness.Application.Common.Interfaces;
using NikaFitness.Application.Common.Models;
using NikaFitness.Application.Receivables.Dtos;
using NikaFitness.Domain.Receivables;

namespace NikaFitness.Application.Receivables.Queries;

public sealed record GetInvoicesQuery(
    InvoiceStatus? Status = null,
    string? Search = null,
    bool? OverdueOnly = null,
    int Page = 1,
    int PageSize = 20) : IRequest<PagedResult<InvoiceSummaryDto>>;

public sealed class GetInvoicesQueryHandler(IApplicationDbContext db)
    : IRequestHandler<GetInvoicesQuery, PagedResult<InvoiceSummaryDto>>
{
    public async Task<PagedResult<InvoiceSummaryDto>> Handle(
        GetInvoicesQuery request,
        CancellationToken cancellationToken)
    {
        var page = request.Page < 1 ? 1 : request.Page;
        var pageSize = request.PageSize is < 1 or > 100 ? 20 : request.PageSize;

        var query = db.Invoices
            .AsNoTracking()
            .Include(i => i.Lines)
            .Include(i => i.Payments)
            .AsQueryable();

        if (request.Status.HasValue)
            query = query.Where(i => i.Status == request.Status.Value);

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var term = request.Search.Trim().ToLower();
            query = query.Where(i =>
                i.CustomerName.ToLower().Contains(term)
                || i.InvoiceNumber.ToLower().Contains(term));
        }

        var totalCount = await query.CountAsync(cancellationToken);

        var invoices = await query
            .OrderByDescending(i => i.IssueDate)
            .ThenByDescending(i => i.CreatedAtUtc)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        var items = invoices
            .Where(i => request.OverdueOnly != true || i.IsOverdue)
            .Select(InvoiceMapping.ToSummary)
            .ToList();

        return new PagedResult<InvoiceSummaryDto>(items, page, pageSize, totalCount);
    }
}

public sealed record GetInvoiceByIdQuery(Guid Id) : IRequest<InvoiceDetailDto>;

public sealed class GetInvoiceByIdQueryHandler(IApplicationDbContext db)
    : IRequestHandler<GetInvoiceByIdQuery, InvoiceDetailDto>
{
    public async Task<InvoiceDetailDto> Handle(
        GetInvoiceByIdQuery request,
        CancellationToken cancellationToken)
    {
        var invoice = await db.Invoices
            .AsNoTracking()
            .Include(i => i.Lines)
            .Include(i => i.Payments)
            .FirstOrDefaultAsync(i => i.Id == request.Id, cancellationToken)
            ?? throw new NotFoundException("Invoice", request.Id);

        return InvoiceMapping.ToDetail(invoice);
    }
}

public sealed record GetArSummaryQuery : IRequest<ArSummaryDto>;

public sealed class GetArSummaryQueryHandler(IApplicationDbContext db)
    : IRequestHandler<GetArSummaryQuery, ArSummaryDto>
{
    public async Task<ArSummaryDto> Handle(
        GetArSummaryQuery request,
        CancellationToken cancellationToken)
    {
        var openStatuses = new[]
        {
            InvoiceStatus.Sent,
            InvoiceStatus.PartiallyPaid
        };

        var invoices = await db.Invoices
            .AsNoTracking()
            .Include(i => i.Lines)
            .Include(i => i.Payments)
            .Where(i => openStatuses.Contains(i.Status))
            .ToListAsync(cancellationToken);

        var currency = invoices.FirstOrDefault()?.Currency
            ?? (await db.CompanySettings.AsNoTracking()
                .Select(s => s.Currency)
                .FirstOrDefaultAsync(cancellationToken)) ?? "KES";

        var outstanding = invoices.Sum(i => i.AmountDue.Amount);
        var overdueInvoices = invoices.Where(i => i.IsOverdue).ToList();
        var overdue = overdueInvoices.Sum(i => i.AmountDue.Amount);

        return new ArSummaryDto(
            currency,
            outstanding,
            overdue,
            invoices.Count,
            overdueInvoices.Count);
    }
}
