using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NikaFitness.Domain.Expenses;
using NikaFitness.Domain.ValueObjects;

namespace NikaFitness.Infrastructure.Persistence.Configurations;

public sealed class ExpenseCategoryConfiguration : IEntityTypeConfiguration<ExpenseCategory>
{
    public void Configure(EntityTypeBuilder<ExpenseCategory> builder)
    {
        builder.ToTable("expense_categories");
        builder.HasKey(c => c.Id);
        builder.Property(c => c.Id).ValueGeneratedNever();

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
