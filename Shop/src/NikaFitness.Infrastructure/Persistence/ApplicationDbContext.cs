using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using NikaFitness.Application.Common.Interfaces;
using NikaFitness.Domain.Auditing;
using NikaFitness.Domain.Catalog;
using NikaFitness.Domain.Common;
using NikaFitness.Domain.Customers;
using NikaFitness.Domain.Expenses;
using NikaFitness.Domain.Orders;
using NikaFitness.Domain.Payments;
using NikaFitness.Domain.Purchasing;
using NikaFitness.Domain.Receivables;
using NikaFitness.Domain.Sequences;
using NikaFitness.Domain.Settings;
using NikaFitness.Infrastructure.Identity;

namespace NikaFitness.Infrastructure.Persistence;

/// <summary>
/// The single EF Core context. Doubles as the ASP.NET Identity store and as the
/// <see cref="IApplicationDbContext"/> the Application layer depends on.
/// </summary>
public sealed class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
    : IdentityDbContext<ApplicationUser, IdentityRole<Guid>, Guid>(options), IApplicationDbContext
{
    public DbSet<Product> Products => Set<Product>();
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<Order> Orders => Set<Order>();
    public DbSet<Payment> Payments => Set<Payment>();
    public DbSet<CompanySettings> CompanySettings => Set<CompanySettings>();
    public DbSet<NumberSequence> NumberSequences => Set<NumberSequence>();
    public DbSet<ExpenseCategory> ExpenseCategories => Set<ExpenseCategory>();
    public DbSet<Bill> Bills => Set<Bill>();
    public DbSet<Invoice> Invoices => Set<Invoice>();
    public DbSet<Vendor> Vendors => Set<Vendor>();
    public DbSet<PurchaseOrder> PurchaseOrders => Set<PurchaseOrder>();
    public DbSet<Customer> Customers => Set<Customer>();
    public DbSet<AuditLogEntry> AuditLog => Set<AuditLogEntry>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
        builder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly);

        // Give Identity tables snake-case names to match the rest of the schema.
        builder.Entity<ApplicationUser>().ToTable("users");
        builder.Entity<IdentityRole<Guid>>().ToTable("roles");
        builder.Entity<IdentityUserRole<Guid>>().ToTable("user_roles");
        builder.Entity<IdentityUserClaim<Guid>>().ToTable("user_claims");
        builder.Entity<IdentityUserLogin<Guid>>().ToTable("user_logins");
        builder.Entity<IdentityUserToken<Guid>>().ToTable("user_tokens");
        builder.Entity<IdentityRoleClaim<Guid>>().ToTable("role_claims");
    }

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        // Domain events are recorded on aggregates; clear them after persistence.
        // (Hook a dispatcher here later to publish them to handlers.)
        var result = await base.SaveChangesAsync(cancellationToken);

        foreach (var aggregate in ChangeTracker.Entries<AggregateRoot>())
            aggregate.Entity.ClearDomainEvents();

        return result;
    }
}
