using MediatR;
using Microsoft.EntityFrameworkCore;
using NikaFitness.Application.Common.Exceptions;
using NikaFitness.Application.Common.Interfaces;
using NikaFitness.Application.Orders.Dtos;

namespace NikaFitness.Application.Orders.Queries;

public sealed record GetOrderByIdQuery(Guid OrderId) : IRequest<OrderDto>;

public sealed class GetOrderByIdQueryHandler(IApplicationDbContext db)
    : IRequestHandler<GetOrderByIdQuery, OrderDto>
{
    public async Task<OrderDto> Handle(GetOrderByIdQuery request, CancellationToken cancellationToken)
    {
        var order = await db.Orders
            .AsNoTracking()
            .Where(o => o.Id == request.OrderId)
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
            .FirstOrDefaultAsync(cancellationToken)
            ?? throw new NotFoundException("Order", request.OrderId);

        return order;
    }
}
