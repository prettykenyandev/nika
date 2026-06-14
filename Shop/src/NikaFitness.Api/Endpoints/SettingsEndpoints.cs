using MediatR;
using NikaFitness.Application.Common.Interfaces;
using NikaFitness.Application.Settings.Commands;
using NikaFitness.Application.Settings.Queries;

namespace NikaFitness.Api.Endpoints;

/// <summary>Company settings and file uploads. Restricted to administrators.</summary>
public static class SettingsEndpoints
{
    public static IEndpointRouteBuilder MapSettingsEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/admin")
            .WithTags("Settings")
            .RequireAuthorization(policy => policy.RequireRole(AuthEndpoints.AdminRole));

        group.MapGet("/settings", async (ISender sender) =>
            Results.Ok(await sender.Send(new GetCompanySettingsQuery())));

        group.MapPut("/settings", async (UpdateCompanySettingsCommand command, ISender sender) =>
        {
            await sender.Send(command);
            return Results.NoContent();
        });

        // Generic file upload (receipts, logos, document attachments).
        group.MapPost("/files", async (HttpRequest request, IFileStorage storage) =>
        {
            if (!request.HasFormContentType)
                return Results.BadRequest(new { message = "Expected a multipart/form-data upload." });

            var form = await request.ReadFormAsync();
            var file = form.Files.GetFile("file") ?? form.Files.FirstOrDefault();
            if (file is null || file.Length == 0)
                return Results.BadRequest(new { message = "No file was uploaded." });

            const long maxBytes = 10 * 1024 * 1024; // 10 MB
            if (file.Length > maxBytes)
                return Results.BadRequest(new { message = "File exceeds the 10 MB limit." });

            await using var stream = file.OpenReadStream();
            var url = await storage.SaveAsync(stream, file.FileName, file.ContentType);
            return Results.Ok(new { url });
        }).DisableAntiforgery();

        return app;
    }
}
