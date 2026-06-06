using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using NikaFitness.Api.Endpoints;
using NikaFitness.Infrastructure.Identity;
using NikaFitness.Infrastructure.Persistence;

namespace NikaFitness.Api.Common;

public static class StartupExtensions
{
    /// <summary>Applies migrations, ensures roles + an admin account, and seeds the catalogue.</summary>
    public static async Task SeedDatabaseAsync(this WebApplication app)
    {
        await using var scope = app.Services.CreateAsyncScope();
        var services = scope.ServiceProvider;

        // Create the database and apply migrations BEFORE any Identity queries run,
        // otherwise the first role/user lookup hits a database that doesn't exist yet.
        var db = services.GetRequiredService<ApplicationDbContext>();
        await db.Database.MigrateAsync();

        var roleManager = services.GetRequiredService<RoleManager<IdentityRole<Guid>>>();
        foreach (var role in new[] { AuthEndpoints.AdminRole, AuthEndpoints.CustomerRole })
        {
            if (!await roleManager.RoleExistsAsync(role))
                await roleManager.CreateAsync(new IdentityRole<Guid>(role));
        }

        var config = services.GetRequiredService<IConfiguration>();
        var adminEmail = config["Admin:Email"] ?? "admin@nikafitness.local";
        var adminPassword = config["Admin:Password"] ?? "Admin123!";

        var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();
        if (await userManager.FindByEmailAsync(adminEmail) is null)
        {
            var admin = new ApplicationUser
            {
                UserName = adminEmail,
                Email = adminEmail,
                EmailConfirmed = true,
                FullName = "Nika Admin"
            };
            var result = await userManager.CreateAsync(admin, adminPassword);
            if (result.Succeeded)
                await userManager.AddToRoleAsync(admin, AuthEndpoints.AdminRole);
        }

        var seeder = services.GetRequiredService<ApplicationDbContextSeeder>();
        await seeder.SeedAsync();
    }
}
