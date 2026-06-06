using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NikaFitness.Domain.Catalog;
using NikaFitness.Domain.ValueObjects;

namespace NikaFitness.Infrastructure.Persistence.Configurations;

public sealed class CategoryConfiguration : IEntityTypeConfiguration<Category>
{
    public void Configure(EntityTypeBuilder<Category> builder)
    {
        builder.ToTable("categories");
        builder.HasKey(c => c.Id);

        builder.Property(c => c.Name).IsRequired().HasMaxLength(120);
        builder.Property(c => c.Description).HasMaxLength(1000);

        builder.Property(c => c.Slug)
            .HasConversion(s => s.Value, v => Slug.FromExisting(v))
            .IsRequired()
            .HasMaxLength(140);

        builder.HasIndex(c => c.Slug).IsUnique();

        builder.Ignore(c => c.DomainEvents);
    }
}
