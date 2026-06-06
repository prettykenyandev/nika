using MediatR;
using NikaFitness.Application.Catalog.Commands;

namespace NikaFitness.Api.Endpoints;

/// <summary>Catalogue management. Restricted to administrators.</summary>
public static class AdminEndpoints
{
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

        return app;
    }
}
