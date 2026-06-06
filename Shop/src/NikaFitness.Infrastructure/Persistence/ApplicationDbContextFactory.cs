using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace NikaFitness.Infrastructure.Persistence;

/// <summary>
/// Lets the EF Core tooling (<c>dotnet ef migrations</c>) build the context without a
/// running database or the Aspire host. The connection string is a design-time placeholder.
/// </summary>
public sealed class ApplicationDbContextFactory : IDesignTimeDbContextFactory<ApplicationDbContext>
{
    public ApplicationDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseNpgsql("Host=localhost;Port=5432;Database=nikadb;Username=postgres;Password=postgres")
            .Options;

        return new ApplicationDbContext(options);
    }
}
