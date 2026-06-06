using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NikaFitness.Domain.Orders;

namespace NikaFitness.Infrastructure.Persistence.Configurations;

public sealed class OrderConfiguration : IEntityTypeConfiguration<Order>
{
    public void Configure(EntityTypeBuilder<Order> builder)
    {
        builder.ToTable("orders");
        builder.HasKey(o => o.Id);

        builder.Property(o => o.OrderNumber).IsRequired().HasMaxLength(40);
        builder.HasIndex(o => o.OrderNumber).IsUnique();

        builder.Property(o => o.CustomerEmail).IsRequired().HasMaxLength(256);
        builder.Property(o => o.CustomerId);
        builder.Property(o => o.Currency).IsRequired().HasMaxLength(3);
        builder.Property(o => o.Status).HasConversion<string>().HasMaxLength(20);
        builder.Property(o => o.CreatedAtUtc).IsRequired();
        builder.Property(o => o.PaidAtUtc);

        builder.HasIndex(o => o.CustomerId);

        builder.OwnsOne(o => o.ShippingAddress, addr =>
        {
            addr.Property(a => a.FullName).HasColumnName("ship_full_name").IsRequired().HasMaxLength(150);
            addr.Property(a => a.Line1).HasColumnName("ship_line1").IsRequired().HasMaxLength(200);
            addr.Property(a => a.Line2).HasColumnName("ship_line2").HasMaxLength(200);
            addr.Property(a => a.City).HasColumnName("ship_city").IsRequired().HasMaxLength(100);
            addr.Property(a => a.PostalCode).HasColumnName("ship_postal_code").HasMaxLength(20);
            addr.Property(a => a.Country).HasColumnName("ship_country").IsRequired().HasMaxLength(100);
            addr.Property(a => a.PhoneNumber).HasColumnName("ship_phone").IsRequired().HasMaxLength(20);
        });
        builder.Navigation(o => o.ShippingAddress).IsRequired();

        builder.HasMany(o => o.Items)
            .WithOne()
            .HasForeignKey(i => i.OrderId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Navigation(o => o.Items)
            .UsePropertyAccessMode(PropertyAccessMode.Field)
            .HasField("_items");

        builder.Ignore(o => o.Total);
        builder.Ignore(o => o.DomainEvents);
    }
}

public sealed class OrderItemConfiguration : IEntityTypeConfiguration<OrderItem>
{
    public void Configure(EntityTypeBuilder<OrderItem> builder)
    {
        builder.ToTable("order_items");
        builder.HasKey(i => i.Id);

        builder.Property(i => i.ProductId).IsRequired();
        builder.Property(i => i.ProductVariantId).IsRequired();
        builder.Property(i => i.ProductName).IsRequired().HasMaxLength(200);
        builder.Property(i => i.Sku).IsRequired().HasMaxLength(64);
        builder.Property(i => i.Quantity).IsRequired();

        builder.OwnsOne(i => i.UnitPrice, price =>
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
        builder.Navigation(i => i.UnitPrice).IsRequired();

        builder.Ignore(i => i.LineTotal);
    }
}
