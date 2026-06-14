using MediatR;
using Microsoft.EntityFrameworkCore;
using NikaFitness.Application.Common.Exceptions;
using NikaFitness.Application.Common.Interfaces;
using NikaFitness.Application.Common.Models;
using NikaFitness.Application.Customers.Dtos;

namespace NikaFitness.Application.Customers.Queries;

public sealed record GetCustomersQuery(
    string? Search = null,
    bool? ActiveOnly = null,
    int Page = 1,
    int PageSize = 100) : IRequest<PagedResult<CustomerSummaryDto>>;

public sealed class GetCustomersQueryHandler(IApplicationDbContext db)
    : IRequestHandler<GetCustomersQuery, PagedResult<CustomerSummaryDto>>
{
    public async Task<PagedResult<CustomerSummaryDto>> Handle(
        GetCustomersQuery request,
        CancellationToken cancellationToken)
    {
        var page = request.Page < 1 ? 1 : request.Page;
        var pageSize = request.PageSize is < 1 or > 200 ? 100 : request.PageSize;

        var query = db.Customers.AsNoTracking().AsQueryable();

        if (request.ActiveOnly == true)
            query = query.Where(c => c.IsActive);

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var term = request.Search.Trim().ToLower();
            query = query.Where(c =>
                c.Name.ToLower().Contains(term)
                || (c.Email != null && c.Email.ToLower().Contains(term))
                || (c.Phone != null && c.Phone.ToLower().Contains(term)));
        }

        var totalCount = await query.CountAsync(cancellationToken);

        var customers = await query
            .OrderBy(c => c.Name)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        var ids = customers.Select(c => c.Id).ToList();

        var orderCounts = await db.Orders.AsNoTracking()
            .Where(o => o.CustomerId != null && ids.Contains(o.CustomerId!.Value))
            .GroupBy(o => o.CustomerId!.Value)
            .Select(g => new { CustomerId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.CustomerId, x => x.Count, cancellationToken);

        var invoices = await db.Invoices.AsNoTracking()
            .Include(i => i.Lines)
            .Include(i => i.Payments)
            .Where(i => i.CustomerId != null && ids.Contains(i.CustomerId!.Value))
            .ToListAsync(cancellationToken);

        var outstandingByCustomer = invoices
            .GroupBy(i => i.CustomerId!.Value)
            .ToDictionary(g => g.Key, g => g.Sum(i => i.AmountDue.Amount));

        var currency = await db.CompanySettings.AsNoTracking()
            .Select(s => s.Currency)
            .FirstOrDefaultAsync(cancellationToken) ?? "KES";

        var items = customers.Select(c => new CustomerSummaryDto(
            c.Id,
            c.Name,
            c.Email,
            c.Phone,
            c.City,
            c.IsActive,
            orderCounts.TryGetValue(c.Id, out var oc) ? oc : 0,
            outstandingByCustomer.TryGetValue(c.Id, out var ob) ? ob : 0,
            currency)).ToList();

        return new PagedResult<CustomerSummaryDto>(items, page, pageSize, totalCount);
    }
}

public sealed record GetCustomerByIdQuery(Guid Id) : IRequest<CustomerDetailDto>;

public sealed class GetCustomerByIdQueryHandler(IApplicationDbContext db)
    : IRequestHandler<GetCustomerByIdQuery, CustomerDetailDto>
{
    public async Task<CustomerDetailDto> Handle(
        GetCustomerByIdQuery request,
        CancellationToken cancellationToken)
    {
        var customer = await db.Customers.AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == request.Id, cancellationToken)
            ?? throw new NotFoundException("Customer", request.Id);

        var orders = await db.Orders.AsNoTracking()
            .Include(o => o.Items)
            .Where(o => o.CustomerId == customer.Id)
            .OrderByDescending(o => o.CreatedAtUtc)
            .ToListAsync(cancellationToken);

        var invoices = await db.Invoices.AsNoTracking()
            .Include(i => i.Lines)
            .Include(i => i.Payments)
            .Where(i => i.CustomerId == customer.Id)
            .OrderByDescending(i => i.IssueDate)
            .ToListAsync(cancellationToken);

        var currency = invoices.FirstOrDefault()?.Currency
            ?? orders.FirstOrDefault()?.Currency
            ?? await db.CompanySettings.AsNoTracking()
                .Select(s => s.Currency)
                .FirstOrDefaultAsync(cancellationToken) ?? "KES";

        var orderDtos = orders.Select(o => new CustomerOrderDto(
            o.Id,
            o.OrderNumber,
            o.CreatedAtUtc,
            o.Status.ToString(),
            o.Total.Amount,
            o.Currency)).ToList();

        var invoiceDtos = invoices.Select(i => new CustomerInvoiceDto(
            i.Id,
            i.InvoiceNumber,
            i.IssueDate,
            i.DueDate,
            i.Status.ToString(),
            i.IsOverdue,
            i.Total.Amount,
            i.AmountDue.Amount,
            i.Currency)).ToList();

        return new CustomerDetailDto(
            customer.Id,
            customer.Name,
            customer.Email,
            customer.Phone,
            customer.AddressLine1,
            customer.City,
            customer.Country,
            customer.Notes,
            customer.IsActive,
            currency,
            invoices.Sum(i => i.Total.Amount),
            invoices.Sum(i => i.AmountDue.Amount),
            orders.Sum(o => o.Total.Amount),
            orderDtos,
            invoiceDtos);
    }
}
