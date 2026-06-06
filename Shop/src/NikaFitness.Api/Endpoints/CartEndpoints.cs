using MediatR;
using NikaFitness.Application.Carts.Commands;
using NikaFitness.Application.Carts.Queries;

namespace NikaFitness.Api.Endpoints;

public static class CartEndpoints
{
    public const string CartIdHeader = "X-Cart-Id";

    public sealed record AddToCartRequest(Guid ProductVariantId, int Quantity);
    public sealed record UpdateCartItemRequest(int Quantity);

    public static IEndpointRouteBuilder MapCartEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/cart").WithTags("Cart");

        group.MapGet("/", async (HttpContext http, ISender sender) =>
        {
            var cartId = ResolveCartId(http);
            return Results.Ok(await sender.Send(new GetCartQuery(cartId)));
        });

        group.MapPost("/items", async (AddToCartRequest request, HttpContext http, ISender sender) =>
        {
            var cartId = ResolveCartId(http);
            await sender.Send(new AddToCartCommand(cartId, request.ProductVariantId, request.Quantity));
            return Results.Ok(await sender.Send(new GetCartQuery(cartId)));
        });

        group.MapPut("/items/{variantId:guid}", async (
            Guid variantId, UpdateCartItemRequest request, HttpContext http, ISender sender) =>
        {
            var cartId = ResolveCartId(http);
            await sender.Send(new UpdateCartItemCommand(cartId, variantId, request.Quantity));
            return Results.Ok(await sender.Send(new GetCartQuery(cartId)));
        });

        group.MapDelete("/items/{variantId:guid}", async (
            Guid variantId, HttpContext http, ISender sender) =>
        {
            var cartId = ResolveCartId(http);
            await sender.Send(new RemoveCartItemCommand(cartId, variantId));
            return Results.Ok(await sender.Send(new GetCartQuery(cartId)));
        });

        return app;
    }

    /// <summary>
    /// The storefront generates a stable cart id and sends it via the X-Cart-Id header.
    /// If absent we mint one and echo it back so the client can persist it.
    /// </summary>
    private static string ResolveCartId(HttpContext http)
    {
        if (http.Request.Headers.TryGetValue(CartIdHeader, out var value) &&
            !string.IsNullOrWhiteSpace(value))
        {
            return value.ToString();
        }

        var cartId = Guid.NewGuid().ToString("N");
        http.Response.Headers[CartIdHeader] = cartId;
        return cartId;
    }
}
