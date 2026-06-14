using MediatR;
using NikaFitness.Application.Common.Interfaces;
using NikaFitness.Application.Receivables.Commands;
using NikaFitness.Application.Receivables.Queries;
using NikaFitness.Application.Settings.Queries;
using NikaFitness.Domain.Receivables;

namespace NikaFitness.Api.Endpoints;

/// <summary>Accounts-receivable endpoints: customer invoices and invoice receipts.</summary>
public static class InvoiceEndpoints
{
    public static IEndpointRouteBuilder MapInvoiceEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/admin")
            .WithTags("Invoices")
            .RequireAuthorization(policy => policy.RequireRole(AuthEndpoints.AdminRole));

        group.MapGet("/invoices/summary", async (ISender sender) =>
            Results.Ok(await sender.Send(new GetArSummaryQuery())));

        group.MapGet("/invoices", async (
            ISender sender,
            string? status,
            string? search,
            bool? overdueOnly,
            int page = 1,
            int pageSize = 20) =>
        {
            InvoiceStatus? parsedStatus =
                Enum.TryParse<InvoiceStatus>(status, true, out var s) ? s : null;
            var result = await sender.Send(
                new GetInvoicesQuery(parsedStatus, search, overdueOnly, page, pageSize));
            return Results.Ok(result);
        });

        group.MapGet("/invoices/{id:guid}", async (Guid id, ISender sender) =>
            Results.Ok(await sender.Send(new GetInvoiceByIdQuery(id))));

        group.MapGet("/invoices/{id:guid}/pdf", async (
            Guid id, ISender sender, IDocumentPdfService pdf) =>
        {
            var invoice = await sender.Send(new GetInvoiceByIdQuery(id));
            var company = await sender.Send(new GetCompanySettingsQuery());
            var bytes = pdf.RenderInvoice(invoice, company);
            return Results.File(bytes, "application/pdf", $"{invoice.InvoiceNumber}.pdf");
        });

        group.MapPost("/invoices/{id:guid}/email", async (
            Guid id, EmailInvoiceRequest? body, ISender sender) =>
        {
            await sender.Send(new EmailInvoiceCommand(id, body?.Email));
            return Results.NoContent();
        });

        group.MapPost("/invoices", async (CreateInvoiceCommand command, ISender sender) =>
        {
            var id = await sender.Send(command);
            return Results.Created($"/api/admin/invoices/{id}", new { id });
        });

        group.MapPost("/invoices/from-order", async (
            CreateInvoiceFromOrderRequest body, ISender sender) =>
        {
            var id = await sender.Send(
                new CreateInvoiceFromOrderCommand(body.OrderId, body.PaymentTermDays));
            return Results.Created($"/api/admin/invoices/{id}", new { id });
        });

        group.MapPost("/invoices/{id:guid}/send", async (Guid id, ISender sender) =>
        {
            await sender.Send(new SendInvoiceCommand(id));
            return Results.NoContent();
        });

        group.MapPost("/invoices/{id:guid}/void", async (Guid id, ISender sender) =>
        {
            await sender.Send(new VoidInvoiceCommand(id));
            return Results.NoContent();
        });

        group.MapPost("/invoices/{id:guid}/payments", async (
            Guid id, RecordInvoicePaymentRequest body, ISender sender) =>
        {
            await sender.Send(new RecordInvoicePaymentCommand(
                id, body.Amount, body.ReceivedOn, body.Method, body.Reference));
            return Results.NoContent();
        });

        return app;
    }
}

/// <summary>Request body for generating an invoice from an existing order.</summary>
public sealed record CreateInvoiceFromOrderRequest(Guid OrderId, int? PaymentTermDays);

/// <summary>Optional override recipient when emailing an invoice.</summary>
public sealed record EmailInvoiceRequest(string? Email);

/// <summary>Request body for recording a receipt against an invoice (id comes from the route).</summary>
public sealed record RecordInvoicePaymentRequest(
    decimal Amount,
    DateOnly ReceivedOn,
    string Method,
    string? Reference);
