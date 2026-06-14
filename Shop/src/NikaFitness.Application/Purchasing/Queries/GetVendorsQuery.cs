using MediatR;
using Microsoft.EntityFrameworkCore;
using NikaFitness.Application.Common.Exceptions;
using NikaFitness.Application.Common.Interfaces;
using NikaFitness.Application.Common.Models;
using NikaFitness.Application.Purchasing.Dtos;

namespace NikaFitness.Application.Purchasing.Queries;

public sealed record GetVendorsQuery(
    string? Search = null,
    bool? ActiveOnly = null,
    int Page = 1,
    int PageSize = 100) : IRequest<PagedResult<VendorSummaryDto>>;

public sealed class GetVendorsQueryHandler(IApplicationDbContext db)
    : IRequestHandler<GetVendorsQuery, PagedResult<VendorSummaryDto>>
{
    public async Task<PagedResult<VendorSummaryDto>> Handle(
        GetVendorsQuery request,
        CancellationToken cancellationToken)
    {
        var page = request.Page < 1 ? 1 : request.Page;
        var pageSize = request.PageSize is < 1 or > 200 ? 100 : request.PageSize;

        var query = db.Vendors.AsNoTracking().AsQueryable();

        if (request.ActiveOnly == true)
            query = query.Where(v => v.IsActive);

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var term = request.Search.Trim().ToLower();
            query = query.Where(v =>
                v.Name.ToLower().Contains(term)
                || (v.ContactName != null && v.ContactName.ToLower().Contains(term))
                || (v.Email != null && v.Email.ToLower().Contains(term)));
        }

        var totalCount = await query.CountAsync(cancellationToken);

        var vendors = await query
            .OrderBy(v => v.Name)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        var items = vendors.Select(PurchasingMapping.ToSummary).ToList();
        return new PagedResult<VendorSummaryDto>(items, page, pageSize, totalCount);
    }
}

public sealed record GetVendorByIdQuery(Guid Id) : IRequest<VendorDetailDto>;

public sealed class GetVendorByIdQueryHandler(IApplicationDbContext db)
    : IRequestHandler<GetVendorByIdQuery, VendorDetailDto>
{
    public async Task<VendorDetailDto> Handle(
        GetVendorByIdQuery request,
        CancellationToken cancellationToken)
    {
        var vendor = await db.Vendors.AsNoTracking()
            .FirstOrDefaultAsync(v => v.Id == request.Id, cancellationToken)
            ?? throw new NotFoundException("Vendor", request.Id);

        return PurchasingMapping.ToDetail(vendor);
    }
}
