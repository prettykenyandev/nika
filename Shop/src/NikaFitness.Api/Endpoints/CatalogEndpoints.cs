using MediatR;
using NikaFitness.Application.Catalog.Queries;

namespace NikaFitness.Api.Endpoints;

public static class CatalogEndpoints
{
    public static IEndpointRouteBuilder MapCatalogEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/catalog").WithTags("Catalog");

        group.MapGet("/products", async (
            ISender sender,
            string? category,
            string? search,
            int page = 1,
            int pageSize = 12) =>
        {
            var result = await sender.Send(new GetProductsQuery(category, search, page, pageSize));
            return Results.Ok(result);
        });

        group.MapGet("/products/{slug}", async (string slug, ISender sender) =>
            Results.Ok(await sender.Send(new GetProductBySlugQuery(slug))));

        group.MapGet("/categories", async (ISender sender) =>
            Results.Ok(await sender.Send(new GetCategoriesQuery())));

        return app;
    }
}
