using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NikaFitness.Domain.Customers;

namespace NikaFitness.Infrastructure.Persistence.Configurations;

public sealed class CustomerConfiguration : IEntityTypeConfiguration<Customer>
{
    public void Configure(EntityTypeBuilder<Customer> builder)
    {
        builder.ToTable("customers");
        builder.HasKey(c => c.Id);
        builder.Property(c => c.Id).ValueGeneratedNever();

        builder.Property(c => c.Name).IsRequired().HasMaxLength(200);
        builder.Property(c => c.Email).HasMaxLength(256);
        builder.Property(c => c.Phone).HasMaxLength(40);
        builder.Property(c => c.AddressLine1).HasMaxLength(200);
        builder.Property(c => c.City).HasMaxLength(120);
        builder.Property(c => c.Country).HasMaxLength(120);
        builder.Property(c => c.Notes).HasMaxLength(1000);
        builder.Property(c => c.IsActive).IsRequired();
        builder.Property(c => c.CreatedAtUtc).IsRequired();

        builder.HasIndex(c => c.Name);
        builder.HasIndex(c => c.Email);

        builder.Ignore(c => c.DomainEvents);
    }
}
