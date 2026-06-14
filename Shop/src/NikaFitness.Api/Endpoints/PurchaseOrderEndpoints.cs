using MediatR;
using NikaFitness.Application.Purchasing.Commands;
using NikaFitness.Application.Purchasing.Queries;
using NikaFitness.Domain.Purchasing;

namespace NikaFitness.Api.Endpoints;

/// <summary>Purchasing endpoints under <c>/api/admin/purchase-orders</c>.</summary>
public static class PurchaseOrderEndpoints
{
    public static IEndpointRouteBuilder MapPurchaseOrderEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/admin")
            .WithTags("Purchasing")
            .RequireAuthorization(policy => policy.RequireRole(AuthEndpoints.AdminRole));

        group.MapGet("/purchasing/variants", async (ISender sender, string? search) =>
            Results.Ok(await sender.Send(new GetVariantOptionsQuery(search))));

        group.MapGet("/purchase-orders", async (
            ISender sender, string? status, Guid? vendorId, string? search, int page = 1, int pageSize = 50) =>
        {
            PurchaseOrderStatus? parsed = Enum.TryParse<PurchaseOrderStatus>(status, true, out var s) ? s : null;
            return Results.Ok(await sender.Send(
                new GetPurchaseOrdersQuery(parsed, vendorId, search, page, pageSize)));
        });

        group.MapGet("/purchase-orders/{id:guid}", async (Guid id, ISender sender) =>
            Results.Ok(await sender.Send(new GetPurchaseOrderByIdQuery(id))));

        group.MapPost("/purchase-orders", async (CreatePurchaseOrderCommand command, ISender sender) =>
        {
            var id = await sender.Send(command);
            return Results.Created($"/api/admin/purchase-orders/{id}", new { id });
        });

        group.MapPost("/purchase-orders/{id:guid}/send", async (Guid id, ISender sender) =>
        {
            await sender.Send(new SendPurchaseOrderCommand(id));
            return Results.NoContent();
        });

        group.MapPost("/purchase-orders/{id:guid}/receive", async (
            Guid id, ReceivePurchaseOrderRequest body, ISender sender) =>
        {
            await sender.Send(new ReceivePurchaseOrderCommand(id, body.Receipts));
            return Results.NoContent();
        });

        group.MapPost("/purchase-orders/{id:guid}/close", async (
            Guid id, ClosePurchaseOrderRequest? body, ISender sender) =>
        {
            var billId = await sender.Send(
                new ClosePurchaseOrderCommand(id, body?.GenerateBill ?? false));
            return Results.Ok(new { billId });
        });

        group.MapPost("/purchase-orders/{id:guid}/cancel", async (Guid id, ISender sender) =>
        {
            await sender.Send(new CancelPurchaseOrderCommand(id));
            return Results.NoContent();
        });

        return app;
    }
}

public sealed record ReceivePurchaseOrderRequest(IReadOnlyList<ReceiptItemInput> Receipts);

public sealed record ClosePurchaseOrderRequest(bool GenerateBill);
