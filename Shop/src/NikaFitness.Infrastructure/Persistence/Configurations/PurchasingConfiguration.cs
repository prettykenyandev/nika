using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NikaFitness.Domain.Purchasing;

namespace NikaFitness.Infrastructure.Persistence.Configurations;

public sealed class VendorConfiguration : IEntityTypeConfiguration<Vendor>
{
    public void Configure(EntityTypeBuilder<Vendor> builder)
    {
        builder.ToTable("vendors");
        builder.HasKey(v => v.Id);
        builder.Property(v => v.Id).ValueGeneratedNever();

        builder.Property(v => v.Name).IsRequired().HasMaxLength(200);
        builder.Property(v => v.ContactName).HasMaxLength(200);
        builder.Property(v => v.Email).HasMaxLength(256);
        builder.Property(v => v.Phone).HasMaxLength(40);
        builder.Property(v => v.AddressLine1).HasMaxLength(200);
        builder.Property(v => v.City).HasMaxLength(120);
        builder.Property(v => v.Country).HasMaxLength(120);
        builder.Property(v => v.TaxIdentifier).HasMaxLength(60);
        builder.Property(v => v.PaymentTermDays).IsRequired();
        builder.Property(v => v.Notes).HasMaxLength(1000);
        builder.Property(v => v.IsActive).IsRequired();
        builder.Property(v => v.CreatedAtUtc).IsRequired();

        builder.HasIndex(v => v.Name);
        builder.HasIndex(v => v.IsActive);

        builder.Ignore(v => v.DomainEvents);
    }
}

public sealed class PurchaseOrderConfiguration : IEntityTypeConfiguration<PurchaseOrder>
{
    public void Configure(EntityTypeBuilder<PurchaseOrder> builder)
    {
        builder.ToTable("purchase_orders");
        builder.HasKey(p => p.Id);
        builder.Property(p => p.Id).ValueGeneratedNever();

        builder.Property(p => p.PoNumber).IsRequired().HasMaxLength(40);
        builder.HasIndex(p => p.PoNumber).IsUnique();

        builder.Property(p => p.VendorId).IsRequired();
        builder.Property(p => p.VendorName).IsRequired().HasMaxLength(200);
        builder.Property(p => p.OrderDate).IsRequired();
        builder.Property(p => p.ExpectedDate);
        builder.Property(p => p.Currency).IsRequired().HasMaxLength(3);
        builder.Property(p => p.Status).HasConversion<string>().HasMaxLength(20);
        builder.Property(p => p.Notes).HasMaxLength(1000);
        builder.Property(p => p.CreatedAtUtc).IsRequired();
        builder.Property(p => p.GeneratedBillId);

        builder.HasIndex(p => p.Status);
        builder.HasIndex(p => p.VendorId);

        builder.HasMany(p => p.Lines)
            .WithOne()
            .HasForeignKey(l => l.PurchaseOrderId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.Navigation(p => p.Lines)
            .UsePropertyAccessMode(PropertyAccessMode.Field)
            .HasField("_lines");

        builder.Ignore(p => p.Subtotal);
        builder.Ignore(p => p.TaxTotal);
        builder.Ignore(p => p.Total);
        builder.Ignore(p => p.DomainEvents);
    }
}

public sealed class PurchaseOrderLineConfiguration : IEntityTypeConfiguration<PurchaseOrderLine>
{
    public void Configure(EntityTypeBuilder<PurchaseOrderLine> builder)
    {
        builder.ToTable("purchase_order_lines");
        builder.HasKey(l => l.Id);
        builder.Property(l => l.Id).ValueGeneratedNever();

        builder.Property(l => l.ProductVariantId);
        builder.Property(l => l.Description).IsRequired().HasMaxLength(300);
        builder.Property(l => l.Sku).HasMaxLength(80);
        builder.Property(l => l.Quantity).HasColumnType("numeric(18,2)").IsRequired();
        builder.Property(l => l.QuantityReceived).HasColumnType("numeric(18,2)").IsRequired();

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

        builder.Ignore(l => l.QuantityOutstanding);
        builder.Ignore(l => l.IsFullyReceived);
        builder.Ignore(l => l.LineNet);
        builder.Ignore(l => l.LineTax);
        builder.Ignore(l => l.LineTotal);
    }
}
