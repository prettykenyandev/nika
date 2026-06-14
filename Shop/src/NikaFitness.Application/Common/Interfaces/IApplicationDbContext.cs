using Microsoft.EntityFrameworkCore;
using NikaFitness.Domain.Catalog;
using NikaFitness.Domain.Expenses;
using NikaFitness.Domain.Orders;
using NikaFitness.Domain.Payments;
using NikaFitness.Domain.Receivables;
using NikaFitness.Domain.Settings;

namespace NikaFitness.Application.Common.Interfaces;

/// <summary>
/// Abstraction over the persistence layer used by command/query handlers. Keeps the
/// Application layer free of a hard dependency on EF Core's concrete DbContext.
/// </summary>
public interface IApplicationDbContext
{
    DbSet<Product> Products { get; }
    DbSet<Category> Categories { get; }
    DbSet<Order> Orders { get; }
    DbSet<Payment> Payments { get; }
    DbSet<CompanySettings> CompanySettings { get; }
    DbSet<ExpenseCategory> ExpenseCategories { get; }
    DbSet<Bill> Bills { get; }
    DbSet<Invoice> Invoices { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken);
}
