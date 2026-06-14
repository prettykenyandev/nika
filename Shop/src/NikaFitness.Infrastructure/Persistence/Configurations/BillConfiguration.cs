using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NikaFitness.Domain.Expenses;

namespace NikaFitness.Infrastructure.Persistence.Configurations;

public sealed class BillConfiguration : IEntityTypeConfiguration<Bill>
{
    public void Configure(EntityTypeBuilder<Bill> builder)
    {
        builder.ToTable("bills");
        builder.HasKey(b => b.Id);
        builder.Property(b => b.Id).ValueGeneratedNever();

        builder.Property(b => b.BillNumber).IsRequired().HasMaxLength(40);
        builder.HasIndex(b => b.BillNumber).IsUnique();

        builder.Property(b => b.VendorName).IsRequired().HasMaxLength(200);
        builder.Property(b => b.VendorId);
        builder.Property(b => b.SupplierReference).HasMaxLength(100);
        builder.Property(b => b.IssueDate).IsRequired();
        builder.Property(b => b.DueDate).IsRequired();
        builder.Property(b => b.Currency).IsRequired().HasMaxLength(3);
        builder.Property(b => b.Status).HasConversion<string>().HasMaxLength(20);
        builder.Property(b => b.Notes).HasMaxLength(1000);
        builder.Property(b => b.AttachmentUrl).HasMaxLength(500);
        builder.Property(b => b.CreatedAtUtc).IsRequired();

        builder.HasIndex(b => b.Status);
        builder.HasIndex(b => b.VendorId);

        builder.HasMany(b => b.Lines)
            .WithOne()
            .HasForeignKey(l => l.BillId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.Navigation(b => b.Lines)
            .UsePropertyAccessMode(PropertyAccessMode.Field)
            .HasField("_lines");

        builder.HasMany(b => b.Payments)
            .WithOne()
            .HasForeignKey(p => p.BillId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.Navigation(b => b.Payments)
            .UsePropertyAccessMode(PropertyAccessMode.Field)
            .HasField("_payments");

        builder.Ignore(b => b.Subtotal);
        builder.Ignore(b => b.TaxTotal);
        builder.Ignore(b => b.Total);
        builder.Ignore(b => b.AmountPaid);
        builder.Ignore(b => b.AmountDue);
        builder.Ignore(b => b.IsOverdue);
        builder.Ignore(b => b.DomainEvents);
    }
}

public sealed class BillLineConfiguration : IEntityTypeConfiguration<BillLine>
{
    public void Configure(EntityTypeBuilder<BillLine> builder)
    {
        builder.ToTable("bill_lines");
        builder.HasKey(l => l.Id);
        builder.Property(l => l.Id).ValueGeneratedNever();

        builder.Property(l => l.Description).IsRequired().HasMaxLength(300);
        builder.Property(l => l.ExpenseCategoryId);
        builder.Property(l => l.Quantity).HasColumnType("numeric(18,2)").IsRequired();

        builder.OwnsOne(l => l.UnitCost, cost =>
        {
            cost.Property(c => c.Amount)
                .HasColumnName("unit_cost")
                .HasColumnType("numeric(18,2)")
                .IsRequired();
            cost.Property(c => c.Currency)
                .HasColumnName("currency")
                .HasMaxLength(3)
                .IsRequired();
        });
        builder.Navigation(l => l.UnitCost).IsRequired();

        builder.OwnsOne(l => l.TaxRate, rate =>
        {
            rate.Property(r => r.Percent)
                .HasColumnName("tax_percent")
                .HasColumnType("numeric(5,2)")
                .IsRequired();
        });
        builder.Navigation(l => l.TaxRate).IsRequired();

        builder.Ignore(l => l.LineNet);
        builder.Ignore(l => l.LineTax);
        builder.Ignore(l => l.LineTotal);
    }
}

public sealed class BillPaymentConfiguration : IEntityTypeConfiguration<BillPayment>
{
    public void Configure(EntityTypeBuilder<BillPayment> builder)
    {
        builder.ToTable("bill_payments");
        builder.HasKey(p => p.Id);
        builder.Property(p => p.Id).ValueGeneratedNever();

        builder.Property(p => p.PaidOn).IsRequired();
        builder.Property(p => p.Method).IsRequired().HasMaxLength(40);
        builder.Property(p => p.Reference).HasMaxLength(100);

        builder.OwnsOne(p => p.Amount, amount =>
        {
            amount.Property(a => a.Amount)
                .HasColumnName("amount")
                .HasColumnType("numeric(18,2)")
                .IsRequired();
            amount.Property(a => a.Currency)
                .HasColumnName("currency")
                .HasMaxLength(3)
                .IsRequired();
        });
        builder.Navigation(p => p.Amount).IsRequired();
    }
}
