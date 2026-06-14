using MediatR;
using Microsoft.EntityFrameworkCore;
using NikaFitness.Application.Common.Exceptions;
using NikaFitness.Application.Common.Interfaces;
using NikaFitness.Application.Common.Models;
using NikaFitness.Application.Expenses.Dtos;
using NikaFitness.Domain.Expenses;

namespace NikaFitness.Application.Expenses.Queries;

public sealed record GetBillsQuery(
    BillStatus? Status = null,
    string? Search = null,
    bool? OverdueOnly = null,
    int Page = 1,
    int PageSize = 20) : IRequest<PagedResult<BillSummaryDto>>;

public sealed class GetBillsQueryHandler(IApplicationDbContext db)
    : IRequestHandler<GetBillsQuery, PagedResult<BillSummaryDto>>
{
    public async Task<PagedResult<BillSummaryDto>> Handle(
        GetBillsQuery request,
        CancellationToken cancellationToken)
    {
        var page = request.Page < 1 ? 1 : request.Page;
        var pageSize = request.PageSize is < 1 or > 100 ? 20 : request.PageSize;

        var query = db.Bills
            .AsNoTracking()
            .Include(b => b.Lines)
            .Include(b => b.Payments)
            .AsQueryable();

        if (request.Status.HasValue)
            query = query.Where(b => b.Status == request.Status.Value);

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var term = request.Search.Trim().ToLower();
            query = query.Where(b =>
                b.VendorName.ToLower().Contains(term)
                || b.BillNumber.ToLower().Contains(term));
        }

        var totalCount = await query.CountAsync(cancellationToken);

        var bills = await query
            .OrderByDescending(b => b.IssueDate)
            .ThenByDescending(b => b.CreatedAtUtc)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        var items = bills
            .Where(b => request.OverdueOnly != true || b.IsOverdue)
            .Select(ExpenseMapping.ToSummary)
            .ToList();

        return new PagedResult<BillSummaryDto>(items, page, pageSize, totalCount);
    }
}

public sealed record GetBillByIdQuery(Guid Id) : IRequest<BillDetailDto>;

public sealed class GetBillByIdQueryHandler(IApplicationDbContext db)
    : IRequestHandler<GetBillByIdQuery, BillDetailDto>
{
    public async Task<BillDetailDto> Handle(
        GetBillByIdQuery request,
        CancellationToken cancellationToken)
    {
        var bill = await db.Bills
            .AsNoTracking()
            .Include(b => b.Lines)
            .Include(b => b.Payments)
            .FirstOrDefaultAsync(b => b.Id == request.Id, cancellationToken)
            ?? throw new NotFoundException("Bill", request.Id);

        var categories = await db.ExpenseCategories
            .AsNoTracking()
            .ToDictionaryAsync(c => c.Id, c => c.Name, cancellationToken);

        return ExpenseMapping.ToDetail(bill, categories);
    }
}

public sealed record GetApSummaryQuery : IRequest<ApSummaryDto>;

public sealed class GetApSummaryQueryHandler(IApplicationDbContext db)
    : IRequestHandler<GetApSummaryQuery, ApSummaryDto>
{
    public async Task<ApSummaryDto> Handle(
        GetApSummaryQuery request,
        CancellationToken cancellationToken)
    {
        var openStatuses = new[]
        {
            BillStatus.AwaitingPayment,
            BillStatus.PartiallyPaid
        };

        var bills = await db.Bills
            .AsNoTracking()
            .Include(b => b.Lines)
            .Include(b => b.Payments)
            .Where(b => openStatuses.Contains(b.Status))
            .ToListAsync(cancellationToken);

        var currency = bills.FirstOrDefault()?.Currency
            ?? (await db.CompanySettings.AsNoTracking()
                .Select(s => s.Currency)
                .FirstOrDefaultAsync(cancellationToken)) ?? "KES";

        var outstanding = bills.Sum(b => b.AmountDue.Amount);
        var overdueBills = bills.Where(b => b.IsOverdue).ToList();
        var overdue = overdueBills.Sum(b => b.AmountDue.Amount);

        return new ApSummaryDto(
            currency,
            outstanding,
            overdue,
            bills.Count,
            overdueBills.Count);
    }
}
