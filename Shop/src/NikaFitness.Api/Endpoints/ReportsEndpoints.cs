using MediatR;
using NikaFitness.Application.Reporting.Queries;

namespace NikaFitness.Api.Endpoints;

/// <summary>Reporting/analytics endpoints under <c>/api/admin/reports</c>.</summary>
public static class ReportsEndpoints
{
    public static IEndpointRouteBuilder MapReportsEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/admin/reports")
            .WithTags("Reports")
            .RequireAuthorization(policy => policy.RequireRole(AuthEndpoints.AdminRole));

        group.MapGet("/dashboard", async (ISender sender) =>
            Results.Ok(await sender.Send(new GetDashboardSummaryQuery())));

        group.MapGet("/profit-and-loss", async (ISender sender, DateOnly? from, DateOnly? to) =>
            Results.Ok(await sender.Send(new GetProfitAndLossQuery(from, to))));

        group.MapGet("/receivables-aging", async (ISender sender) =>
            Results.Ok(await sender.Send(new GetReceivablesAgingQuery())));

        group.MapGet("/payables-aging", async (ISender sender) =>
            Results.Ok(await sender.Send(new GetPayablesAgingQuery())));

        group.MapGet("/sales", async (ISender sender, DateOnly? from, DateOnly? to) =>
            Results.Ok(await sender.Send(new GetSalesAnalyticsQuery(from, to))));

        group.MapGet("/inventory", async (ISender sender) =>
            Results.Ok(await sender.Send(new GetInventoryValuationQuery())));

        group.MapGet("/vat", async (ISender sender, DateOnly? from, DateOnly? to) =>
            Results.Ok(await sender.Send(new GetVatSummaryQuery(from, to))));

        return app;
    }
}
