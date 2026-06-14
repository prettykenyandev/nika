using MediatR;
using NikaFitness.Application.Purchasing.Commands;
using NikaFitness.Application.Purchasing.Queries;

namespace NikaFitness.Api.Endpoints;

/// <summary>Supplier endpoints under <c>/api/admin/vendors</c>.</summary>
public static class VendorEndpoints
{
    public static IEndpointRouteBuilder MapVendorEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/admin")
            .WithTags("Vendors")
            .RequireAuthorization(policy => policy.RequireRole(AuthEndpoints.AdminRole));

        group.MapGet("/vendors", async (
            ISender sender, string? search, bool? activeOnly, int page = 1, int pageSize = 100) =>
            Results.Ok(await sender.Send(new GetVendorsQuery(search, activeOnly, page, pageSize))));

        group.MapGet("/vendors/{id:guid}", async (Guid id, ISender sender) =>
            Results.Ok(await sender.Send(new GetVendorByIdQuery(id))));

        group.MapPost("/vendors", async (VendorInput body, ISender sender) =>
        {
            var id = await sender.Send(new CreateVendorCommand(body));
            return Results.Created($"/api/admin/vendors/{id}", new { id });
        });

        group.MapPut("/vendors/{id:guid}", async (Guid id, VendorInput body, ISender sender) =>
        {
            await sender.Send(new UpdateVendorCommand(id, body));
            return Results.NoContent();
        });

        group.MapPost("/vendors/{id:guid}/activate", async (Guid id, ISender sender) =>
        {
            await sender.Send(new SetVendorActiveCommand(id, true));
            return Results.NoContent();
        });

        group.MapPost("/vendors/{id:guid}/deactivate", async (Guid id, ISender sender) =>
        {
            await sender.Send(new SetVendorActiveCommand(id, false));
            return Results.NoContent();
        });

        return app;
    }
}
