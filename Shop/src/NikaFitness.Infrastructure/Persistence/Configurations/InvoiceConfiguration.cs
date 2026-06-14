using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NikaFitness.Domain.Receivables;

namespace NikaFitness.Infrastructure.Persistence.Configurations;

public sealed class InvoiceConfiguration : IEntityTypeConfiguration<Invoice>
{
    public void Configure(EntityTypeBuilder<Invoice> builder)
    {
        builder.ToTable("invoices");
        builder.HasKey(i => i.Id);
        builder.Property(i => i.Id).ValueGeneratedNever();

        builder.Property(i => i.InvoiceNumber).IsRequired().HasMaxLength(40);
        builder.HasIndex(i => i.InvoiceNumber).IsUnique();

        builder.Property(i => i.CustomerName).IsRequired().HasMaxLength(200);
        builder.Property(i => i.CustomerId);
        builder.Property(i => i.CustomerEmail).HasMaxLength(256);
        builder.Property(i => i.OrderId);
        builder.Property(i => i.IssueDate).IsRequired();
        builder.Property(i => i.DueDate).IsRequired();
        builder.Property(i => i.Currency).IsRequired().HasMaxLength(3);
        builder.Property(i => i.Status).HasConversion<string>().HasMaxLength(20);
        builder.Property(i => i.Notes).HasMaxLength(1000);
        builder.Property(i => i.CreatedAtUtc).IsRequired();
        builder.Property(i => i.SentAtUtc);

        builder.HasIndex(i => i.Status);
        builder.HasIndex(i => i.CustomerId);
        builder.HasIndex(i => i.OrderId);

        builder.HasMany(i => i.Lines)
            .WithOne()
            .HasForeignKey(l => l.InvoiceId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.Navigation(i => i.Lines)
            .UsePropertyAccessMode(PropertyAccessMode.Field)
            .HasField("_lines");

        builder.HasMany(i => i.Payments)
            .WithOne()
            .HasForeignKey(p => p.InvoiceId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.Navigation(i => i.Payments)
            .UsePropertyAccessMode(PropertyAccessMode.Field)
            .HasField("_payments");

        builder.Ignore(i => i.Subtotal);
        builder.Ignore(i => i.TaxTotal);
        builder.Ignore(i => i.Total);
        builder.Ignore(i => i.AmountPaid);
        builder.Ignore(i => i.AmountDue);
        builder.Ignore(i => i.IsOverdue);
        builder.Ignore(i => i.DomainEvents);
    }
}

public sealed class InvoiceLineConfiguration : IEntityTypeConfiguration<InvoiceLine>
{
    public void Configure(EntityTypeBuilder<InvoiceLine> builder)
    {
        builder.ToTable("invoice_lines");
        builder.HasKey(l => l.Id);
        builder.Property(l => l.Id).ValueGeneratedNever();

        builder.Property(l => l.Description).IsRequired().HasMaxLength(300);
        builder.Property(l => l.Quantity).HasColumnType("numeric(18,2)").IsRequired();

        builder.OwnsOne(l => l.UnitPrice, price =>
        {
            price.Property(p => p.Amount)
                .HasColumnName("unit_price")
                .HasColumnType("numeric(18,2)")
                .IsRequired();
            price.Property(p => p.Currency)
                .HasColumnName("currency")
                .HasMaxLength(3)
                .IsRequired();
        });
        builder.Navigation(l => l.UnitPrice).IsRequired();

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

public sealed class InvoicePaymentConfiguration : IEntityTypeConfiguration<InvoicePayment>
{
    public void Configure(EntityTypeBuilder<InvoicePayment> builder)
    {
        builder.ToTable("invoice_payments");
        builder.HasKey(p => p.Id);
        builder.Property(p => p.Id).ValueGeneratedNever();

        builder.Property(p => p.ReceivedOn).IsRequired();
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
