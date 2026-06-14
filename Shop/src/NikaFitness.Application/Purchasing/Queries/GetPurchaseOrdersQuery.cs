using MediatR;
using Microsoft.EntityFrameworkCore;
using NikaFitness.Application.Common.Exceptions;
using NikaFitness.Application.Common.Interfaces;
using NikaFitness.Application.Common.Models;
using NikaFitness.Application.Purchasing.Dtos;
using NikaFitness.Domain.Purchasing;

namespace NikaFitness.Application.Purchasing.Queries;

public sealed record GetPurchaseOrdersQuery(
    PurchaseOrderStatus? Status = null,
    Guid? VendorId = null,
    string? Search = null,
    int Page = 1,
    int PageSize = 50) : IRequest<PagedResult<PurchaseOrderSummaryDto>>;

public sealed class GetPurchaseOrdersQueryHandler(IApplicationDbContext db)
    : IRequestHandler<GetPurchaseOrdersQuery, PagedResult<PurchaseOrderSummaryDto>>
{
    public async Task<PagedResult<PurchaseOrderSummaryDto>> Handle(
        GetPurchaseOrdersQuery request,
        CancellationToken cancellationToken)
    {
        var page = request.Page < 1 ? 1 : request.Page;
        var pageSize = request.PageSize is < 1 or > 100 ? 50 : request.PageSize;

        var query = db.PurchaseOrders
            .AsNoTracking()
            .Include(p => p.Lines)
            .AsQueryable();

        if (request.Status.HasValue)
            query = query.Where(p => p.Status == request.Status.Value);

        if (request.VendorId.HasValue)
            query = query.Where(p => p.VendorId == request.VendorId.Value);

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var term = request.Search.Trim().ToLower();
            query = query.Where(p =>
                p.PoNumber.ToLower().Contains(term)
                || p.VendorName.ToLower().Contains(term));
        }

        var totalCount = await query.CountAsync(cancellationToken);

        var orders = await query
            .OrderByDescending(p => p.OrderDate)
            .ThenByDescending(p => p.CreatedAtUtc)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        var items = orders.Select(PurchasingMapping.ToSummary).ToList();
        return new PagedResult<PurchaseOrderSummaryDto>(items, page, pageSize, totalCount);
    }
}

public sealed record GetPurchaseOrderByIdQuery(Guid Id) : IRequest<PurchaseOrderDetailDto>;

public sealed class GetPurchaseOrderByIdQueryHandler(IApplicationDbContext db)
    : IRequestHandler<GetPurchaseOrderByIdQuery, PurchaseOrderDetailDto>
{
    public async Task<PurchaseOrderDetailDto> Handle(
        GetPurchaseOrderByIdQuery request,
        CancellationToken cancellationToken)
    {
        var po = await db.PurchaseOrders
            .AsNoTracking()
            .Include(p => p.Lines)
            .FirstOrDefaultAsync(p => p.Id == request.Id, cancellationToken)
            ?? throw new NotFoundException("PurchaseOrder", request.Id);

        return PurchasingMapping.ToDetail(po);
    }
}
