using Microsoft.AspNetCore.Identity;
using NikaFitness.Infrastructure.Identity;

namespace NikaFitness.Api.Endpoints;

/// <summary>Staff/user management endpoints under <c>/api/admin/staff</c> (Phase 5).</summary>
public static class StaffEndpoints
{
    public sealed record StaffDto(
        Guid Id, string Email, string FullName, string[] Roles, bool IsActive);

    public sealed record CreateStaffRequest(
        string Email, string FullName, string Password, string[] Roles);

    public sealed record SetRolesRequest(string[] Roles);

    public static IEndpointRouteBuilder MapStaffEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/admin/staff")
            .WithTags("Staff")
            .RequireAuthorization(policy => policy.RequireRole(AuthEndpoints.AdminRole));

        group.MapGet("/roles", () => Results.Ok(AuthEndpoints.StaffRoles));

        group.MapGet("/", async (UserManager<ApplicationUser> users) =>
        {
            var staffRoles = AuthEndpoints.StaffRoles.ToHashSet(StringComparer.OrdinalIgnoreCase);
            var result = new List<StaffDto>();

            foreach (var user in users.Users.ToList())
            {
                var roles = await users.GetRolesAsync(user);
                if (!roles.Any(r => staffRoles.Contains(r)))
                    continue;

                result.Add(new StaffDto(
                    user.Id,
                    user.Email ?? string.Empty,
                    user.FullName,
                    roles.ToArray(),
                    !await users.IsLockedOutAsync(user)));
            }

            return Results.Ok(result.OrderBy(s => s.Email).ToList());
        });

        group.MapPost("/", async (CreateStaffRequest body, UserManager<ApplicationUser> users) =>
        {
            if (string.IsNullOrWhiteSpace(body.Email) || string.IsNullOrWhiteSpace(body.Password))
                return Results.BadRequest(new { error = "Email and password are required." });

            var requestedRoles = NormaliseRoles(body.Roles);
            if (requestedRoles.Count == 0)
                return Results.BadRequest(new { error = "At least one valid staff role is required." });

            if (await users.FindByEmailAsync(body.Email) is not null)
                return Results.Conflict(new { error = "A user with that email already exists." });

            var user = new ApplicationUser
            {
                UserName = body.Email,
                Email = body.Email,
                EmailConfirmed = true,
                FullName = body.FullName ?? string.Empty
            };

            var created = await users.CreateAsync(user, body.Password);
            if (!created.Succeeded)
                return Results.BadRequest(new { error = string.Join("; ", created.Errors.Select(e => e.Description)) });

            await users.AddToRolesAsync(user, requestedRoles);
            return Results.Created($"/api/admin/staff/{user.Id}", new { id = user.Id });
        });

        group.MapPut("/{id:guid}/roles", async (Guid id, SetRolesRequest body, UserManager<ApplicationUser> users) =>
        {
            var user = await users.FindByIdAsync(id.ToString());
            if (user is null)
                return Results.NotFound();

            var requestedRoles = NormaliseRoles(body.Roles);
            if (requestedRoles.Count == 0)
                return Results.BadRequest(new { error = "At least one valid staff role is required." });

            var current = await users.GetRolesAsync(user);
            var staffRoles = AuthEndpoints.StaffRoles.ToHashSet(StringComparer.OrdinalIgnoreCase);
            var currentStaff = current.Where(r => staffRoles.Contains(r)).ToArray();

            await users.RemoveFromRolesAsync(user, currentStaff);
            await users.AddToRolesAsync(user, requestedRoles);
            return Results.NoContent();
        });

        group.MapPost("/{id:guid}/activate", async (Guid id, UserManager<ApplicationUser> users) =>
        {
            var user = await users.FindByIdAsync(id.ToString());
            if (user is null) return Results.NotFound();
            await users.SetLockoutEndDateAsync(user, null);
            return Results.NoContent();
        });

        group.MapPost("/{id:guid}/deactivate", async (Guid id, UserManager<ApplicationUser> users) =>
        {
            var user = await users.FindByIdAsync(id.ToString());
            if (user is null) return Results.NotFound();
            await users.SetLockoutEnabledAsync(user, true);
            await users.SetLockoutEndDateAsync(user, DateTimeOffset.MaxValue);
            return Results.NoContent();
        });

        return app;
    }

    private static List<string> NormaliseRoles(string[]? roles)
    {
        if (roles is null) return new List<string>();
        var staffRoles = AuthEndpoints.StaffRoles;
        return roles
            .Where(r => !string.IsNullOrWhiteSpace(r))
            .Select(r => staffRoles.FirstOrDefault(s => string.Equals(s, r.Trim(), StringComparison.OrdinalIgnoreCase)))
            .Where(r => r is not null)
            .Select(r => r!)
            .Distinct()
            .ToList();
    }
}
