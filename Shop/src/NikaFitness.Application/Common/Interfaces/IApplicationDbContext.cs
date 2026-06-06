using Microsoft.EntityFrameworkCore;
using NikaFitness.Domain.Catalog;
using NikaFitness.Domain.Orders;
using NikaFitness.Domain.Payments;

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

    Task<int> SaveChangesAsync(CancellationToken cancellationToken);
}
