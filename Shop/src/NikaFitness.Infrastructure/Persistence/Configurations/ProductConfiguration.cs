using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NikaFitness.Domain.Catalog;
using NikaFitness.Domain.ValueObjects;

namespace NikaFitness.Infrastructure.Persistence.Configurations;

public sealed class ProductConfiguration : IEntityTypeConfiguration<Product>
{
    public void Configure(EntityTypeBuilder<Product> builder)
    {
        builder.ToTable("products");
        builder.HasKey(p => p.Id);

        builder.Property(p => p.Name).IsRequired().HasMaxLength(200);
        builder.Property(p => p.Description).IsRequired();
        builder.Property(p => p.CategoryId).IsRequired();
        builder.Property(p => p.IsPublished).HasDefaultValue(false);
        builder.Property(p => p.CreatedAtUtc).IsRequired();

        builder.Property(p => p.Slug)
            .HasConversion(s => s.Value, v => Slug.FromExisting(v))
            .IsRequired()
            .HasMaxLength(220);

        builder.HasIndex(p => p.Slug).IsUnique();
        builder.HasIndex(p => p.CategoryId);

        // EF Core 9 maps List<string> as a primitive collection (text[] on Npgsql).
        builder.PrimitiveCollection<List<string>>("_imageUrls")
            .HasColumnName("image_urls");
        builder.Metadata
            .FindNavigation(nameof(Product.Variants))!
            .SetPropertyAccessMode(PropertyAccessMode.Field);

        builder.Ignore(p => p.ImageUrls);
        builder.Ignore(p => p.DomainEvents);

        builder.HasMany(p => p.Variants)
            .WithOne()
            .HasForeignKey(v => v.ProductId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Navigation(p => p.Variants)
            .UsePropertyAccessMode(PropertyAccessMode.Field)
            .HasField("_variants");
    }
}

public sealed class ProductVariantConfiguration : IEntityTypeConfiguration<ProductVariant>
{
    public void Configure(EntityTypeBuilder<ProductVariant> builder)
    {
        builder.ToTable("product_variants");
        builder.HasKey(v => v.Id);

        builder.Property(v => v.Sku).IsRequired().HasMaxLength(64);
        builder.Property(v => v.Name).IsRequired().HasMaxLength(120);
        builder.Property(v => v.StockQuantity).IsRequired();

        builder.HasIndex(v => v.Sku).IsUnique();

        builder.OwnsOne(v => v.Price, price =>
        {
            price.Property(p => p.Amount)
                .HasColumnName("price")
                .HasColumnType("numeric(18,2)")
                .IsRequired();
            price.Property(p => p.Currency)
                .HasColumnName("currency")
                .HasMaxLength(3)
                .IsRequired();
        });
        builder.Navigation(v => v.Price).IsRequired();
    }
}
