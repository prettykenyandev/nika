using MediatR;
using Microsoft.EntityFrameworkCore;
using NikaFitness.Application.Common.Interfaces;
using NikaFitness.Application.Orders.Dtos;

namespace NikaFitness.Application.Orders.Queries;

/// <summary>Order history for the authenticated customer.</summary>
public sealed record GetMyOrdersQuery : IRequest<IReadOnlyList<OrderDto>>;

public sealed class GetMyOrdersQueryHandler(IApplicationDbContext db, ICurrentUser currentUser)
    : IRequestHandler<GetMyOrdersQuery, IReadOnlyList<OrderDto>>
{
    public async Task<IReadOnlyList<OrderDto>> Handle(
        GetMyOrdersQuery request,
        CancellationToken cancellationToken)
    {
        if (currentUser.UserId is null)
            return [];

        return await db.Orders
            .AsNoTracking()
            .Where(o => o.CustomerId == currentUser.UserId)
            .OrderByDescending(o => o.CreatedAtUtc)
            .Select(o => new OrderDto(
                o.Id,
                o.OrderNumber,
                o.CustomerEmail,
                o.Status,
                o.Total.Amount,
                o.Total.Currency,
                o.CreatedAtUtc,
                o.PaidAtUtc,
                o.Items.Select(i => new OrderItemDto(
                    i.ProductName,
                    i.Sku,
                    i.UnitPrice.Amount,
                    i.Quantity,
                    i.LineTotal.Amount)).ToList()))
            .ToListAsync(cancellationToken);
    }
}
