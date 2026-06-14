using MediatR;
using NikaFitness.Application.Customers.Commands;
using NikaFitness.Application.Customers.Dtos;
using NikaFitness.Application.Customers.Queries;

namespace NikaFitness.Api.Endpoints;

/// <summary>Customer/CRM endpoints under <c>/api/admin/customers</c>.</summary>
public static class CustomerEndpoints
{
    public static IEndpointRouteBuilder MapCustomerEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/admin")
            .WithTags("Customers")
            .RequireAuthorization(policy => policy.RequireRole(AuthEndpoints.AdminRole));

        group.MapGet("/customers", async (
            ISender sender, string? search, bool? activeOnly, int page = 1, int pageSize = 100) =>
            Results.Ok(await sender.Send(new GetCustomersQuery(search, activeOnly, page, pageSize))));

        group.MapGet("/customers/{id:guid}", async (Guid id, ISender sender) =>
            Results.Ok(await sender.Send(new GetCustomerByIdQuery(id))));

        group.MapPost("/customers", async (CustomerInput body, ISender sender) =>
        {
            var id = await sender.Send(new CreateCustomerCommand(body));
            return Results.Created($"/api/admin/customers/{id}", new { id });
        });

        group.MapPut("/customers/{id:guid}", async (Guid id, CustomerInput body, ISender sender) =>
        {
            await sender.Send(new UpdateCustomerCommand(id, body));
            return Results.NoContent();
        });

        group.MapPost("/customers/{id:guid}/activate", async (Guid id, ISender sender) =>
        {
            await sender.Send(new SetCustomerActiveCommand(id, true));
            return Results.NoContent();
        });

        group.MapPost("/customers/{id:guid}/deactivate", async (Guid id, ISender sender) =>
        {
            await sender.Send(new SetCustomerActiveCommand(id, false));
            return Results.NoContent();
        });

        group.MapPost("/customers/backfill", async (ISender sender) =>
            Results.Ok(await sender.Send(new BackfillCustomersFromOrdersCommand())));

        return app;
    }
}
