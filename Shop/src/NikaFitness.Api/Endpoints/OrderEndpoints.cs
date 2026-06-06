using MediatR;
using Microsoft.AspNetCore.Mvc;
using NikaFitness.Application.Orders.Commands;
using NikaFitness.Application.Orders.Queries;

namespace NikaFitness.Api.Endpoints;

public static class OrderEndpoints
{
    public sealed record CheckoutRequest(
        string Email,
        string FullName,
        string Line1,
        string City,
        string Country,
        string PhoneNumber,
        string? Line2,
        string? PostalCode);

    public static IEndpointRouteBuilder MapOrderEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/orders").WithTags("Orders");

        group.MapPost("/checkout", async (CheckoutRequest request, HttpContext http, ISender sender) =>
        {
            var cartId = http.Request.Headers[CartEndpoints.CartIdHeader].ToString();
            var result = await sender.Send(new CheckoutCommand(
                cartId,
                request.Email,
                request.FullName,
                request.Line1,
                request.City,
                request.Country,
                request.PhoneNumber,
                request.Line2,
                request.PostalCode));

            return Results.Ok(result);
        });

        group.MapGet("/{id:guid}", async (Guid id, ISender sender) =>
            Results.Ok(await sender.Send(new GetOrderByIdQuery(id))));

        group.MapGet("/mine", async (ISender sender) =>
            Results.Ok(await sender.Send(new GetMyOrdersQuery())))
            .RequireAuthorization();

        return app;
    }
}
