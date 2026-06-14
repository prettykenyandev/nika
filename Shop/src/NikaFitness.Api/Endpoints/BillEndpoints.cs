using MediatR;
using NikaFitness.Application.Expenses.Commands;
using NikaFitness.Application.Expenses.Queries;
using NikaFitness.Domain.Expenses;

namespace NikaFitness.Api.Endpoints;

/// <summary>Accounts-payable endpoints: expense categories, bills and bill payments.</summary>
public static class BillEndpoints
{
    public static IEndpointRouteBuilder MapBillEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/admin")
            .WithTags("Expenses")
            .RequireAuthorization(policy => policy.RequireRole(AuthEndpoints.AdminRole));

        group.MapGet("/expenses/categories", async (ISender sender) =>
            Results.Ok(await sender.Send(new GetExpenseCategoriesQuery())));

        group.MapPost("/expenses/categories", async (
            CreateExpenseCategoryCommand command, ISender sender) =>
        {
            var id = await sender.Send(command);
            return Results.Created($"/api/admin/expenses/categories/{id}", new { id });
        });

        group.MapGet("/expenses/summary", async (ISender sender) =>
            Results.Ok(await sender.Send(new GetApSummaryQuery())));

        group.MapGet("/bills", async (
            ISender sender,
            string? status,
            string? search,
            bool? overdueOnly,
            int page = 1,
            int pageSize = 20) =>
        {
            BillStatus? parsedStatus = Enum.TryParse<BillStatus>(status, true, out var s) ? s : null;
            var result = await sender.Send(
                new GetBillsQuery(parsedStatus, search, overdueOnly, page, pageSize));
            return Results.Ok(result);
        });

        group.MapGet("/bills/{id:guid}", async (Guid id, ISender sender) =>
            Results.Ok(await sender.Send(new GetBillByIdQuery(id))));

        group.MapPost("/bills", async (CreateBillCommand command, ISender sender) =>
        {
            var id = await sender.Send(command);
            return Results.Created($"/api/admin/bills/{id}", new { id });
        });

        group.MapPost("/bills/{id:guid}/approve", async (Guid id, ISender sender) =>
        {
            await sender.Send(new ApproveBillCommand(id));
            return Results.NoContent();
        });

        group.MapPost("/bills/{id:guid}/cancel", async (Guid id, ISender sender) =>
        {
            await sender.Send(new CancelBillCommand(id));
            return Results.NoContent();
        });

        group.MapPost("/bills/{id:guid}/payments", async (
            Guid id, RecordBillPaymentRequest body, ISender sender) =>
        {
            await sender.Send(new RecordBillPaymentCommand(
                id, body.Amount, body.PaidOn, body.Method, body.Reference));
            return Results.NoContent();
        });

        return app;
    }
}

/// <summary>Request body for recording a payment against a bill (bill id comes from the route).</summary>
public sealed record RecordBillPaymentRequest(
    decimal Amount,
    DateOnly PaidOn,
    string Method,
    string? Reference);
