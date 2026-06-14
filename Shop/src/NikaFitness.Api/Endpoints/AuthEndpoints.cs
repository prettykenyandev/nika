using Microsoft.AspNetCore.Identity;
using NikaFitness.Api.Common;
using NikaFitness.Infrastructure.Identity;

namespace NikaFitness.Api.Endpoints;

public static class AuthEndpoints
{
    public const string CustomerRole = "Customer";
    public const string AdminRole = "Admin";

    // Finer-grained staff roles (Phase 5). Admin remains the superset for endpoint access today;
    // these are assignable to staff and surfaced for future per-area policies.
    public const string OwnerRole = "Owner";
    public const string ManagerRole = "Manager";
    public const string CashierRole = "Cashier";
    public const string AccountantRole = "Accountant";

    /// <summary>Roles that represent internal staff (everything except the storefront Customer role).</summary>
    public static readonly string[] StaffRoles =
        { OwnerRole, AdminRole, ManagerRole, CashierRole, AccountantRole };

    public sealed record RegisterRequest(string Email, string Password, string FullName);
    public sealed record LoginRequest(string Email, string Password);
    public sealed record AuthResponse(string Token, DateTime ExpiresAtUtc, string Email, string FullName, string[] Roles);

    public static IEndpointRouteBuilder MapAuthEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/auth").WithTags("Auth");

        group.MapPost("/register", async (
            RegisterRequest request,
            UserManager<ApplicationUser> userManager,
            TokenService tokenService) =>
        {
            var user = new ApplicationUser
            {
                UserName = request.Email,
                Email = request.Email,
                FullName = request.FullName
            };

            var result = await userManager.CreateAsync(user, request.Password);
            if (!result.Succeeded)
                return Results.ValidationProblem(ToErrors(result));

            await userManager.AddToRoleAsync(user, CustomerRole);

            var roles = await userManager.GetRolesAsync(user);
            var (token, expiresAt) = tokenService.CreateToken(user, roles);
            return Results.Ok(new AuthResponse(token, expiresAt, user.Email!, user.FullName, [.. roles]));
        });

        group.MapPost("/login", async (
            LoginRequest request,
            UserManager<ApplicationUser> userManager,
            TokenService tokenService) =>
        {
            var user = await userManager.FindByEmailAsync(request.Email);
            if (user is null || !await userManager.CheckPasswordAsync(user, request.Password))
                return Results.Problem("Invalid email or password.", statusCode: StatusCodes.Status401Unauthorized);

            var roles = await userManager.GetRolesAsync(user);
            var (token, expiresAt) = tokenService.CreateToken(user, roles);
            return Results.Ok(new AuthResponse(token, expiresAt, user.Email!, user.FullName, [.. roles]));
        });

        return app;
    }

    private static Dictionary<string, string[]> ToErrors(IdentityResult result) =>
        result.Errors
            .GroupBy(e => e.Code)
            .ToDictionary(g => g.Key, g => g.Select(e => e.Description).ToArray());
}
