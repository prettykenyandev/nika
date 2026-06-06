using MediatR;
using NikaFitness.Application.Catalog.Commands;
using NikaFitness.Application.Pos.Commands;
using NikaFitness.Application.Pos.Queries;
using NikaFitness.Domain.Payments;

namespace NikaFitness.Api.Endpoints;

/// <summary>Catalogue management. Restricted to administrators.</summary>
public static class AdminEndpoints
{
    public sealed record PosSaleLine(Guid ProductVariantId, int Quantity);

    public sealed record PosSaleRequest(
        IReadOnlyList<PosSaleLine> Items,
        string Method,
        string? CustomerPhone,
        string? CustomerEmail);

    public static IEndpointRouteBuilder MapAdminEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/admin")
            .WithTags("Admin")
            .RequireAuthorization(policy => policy.RequireRole(AuthEndpoints.AdminRole));

        group.MapPost("/categories", async (CreateCategoryCommand command, ISender sender) =>
        {
            var id = await sender.Send(command);
            return Results.Created($"/api/catalog/categories/{id}", new { id });
        });

        group.MapPost("/products", async (CreateProductCommand command, ISender sender) =>
        {
            var id = await sender.Send(command);
            return Results.Created($"/api/catalog/products/{id}", new { id });
        });

        // --- Point of sale (till) ---

        // Resolve a variant by SKU / scanned barcode.
        group.MapGet("/pos/lookup", async (string sku, ISender sender) =>
        {
            var variant = await sender.Send(new GetVariantBySkuQuery(sku));
            return variant is null
                ? Results.NotFound(new { message = $"No product found for SKU '{sku}'." })
                : Results.Ok(variant);
        });

        // Ring up an in-store sale paid by Card or M-Pesa (cash is not accepted).
        group.MapPost("/pos/sales", async (PosSaleRequest request, ISender sender) =>
        {
            if (!Enum.TryParse<PaymentMethod>(request.Method, ignoreCase: true, out var method)
                || method is not (PaymentMethod.Card or PaymentMethod.Mpesa))
            {
                return Results.ValidationProblem(new Dictionary<string, string[]>
                {
                    ["method"] = ["Payment method must be 'Card' or 'Mpesa'."]
                });
            }

            var result = await sender.Send(new CreatePosSaleCommand(
                request.Items.Select(i => new PosLineInput(i.ProductVariantId, i.Quantity)).ToList(),
                method,
                request.CustomerPhone,
                request.CustomerEmail));

            return Results.Ok(result);
        });

        return app;
    }
}
