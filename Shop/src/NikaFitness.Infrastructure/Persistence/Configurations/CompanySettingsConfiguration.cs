using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NikaFitness.Domain.Settings;

namespace NikaFitness.Infrastructure.Persistence.Configurations;

public sealed class CompanySettingsConfiguration : IEntityTypeConfiguration<CompanySettings>
{
    public void Configure(EntityTypeBuilder<CompanySettings> builder)
    {
        builder.ToTable("company_settings");
        builder.HasKey(s => s.Id);

        builder.Property(s => s.LegalName).IsRequired().HasMaxLength(200);
        builder.Property(s => s.TradingName).HasMaxLength(200);
        builder.Property(s => s.Email).HasMaxLength(256);
        builder.Property(s => s.Phone).HasMaxLength(40);
        builder.Property(s => s.TaxIdentifier).HasMaxLength(60);
        builder.Property(s => s.AddressLine1).HasMaxLength(200);
        builder.Property(s => s.AddressLine2).HasMaxLength(200);
        builder.Property(s => s.City).HasMaxLength(100);
        builder.Property(s => s.Country).HasMaxLength(100);
        builder.Property(s => s.LogoUrl).HasMaxLength(500);
        builder.Property(s => s.Currency).IsRequired().HasMaxLength(3);
        builder.Property(s => s.InvoiceNumberPrefix).IsRequired().HasMaxLength(10);
        builder.Property(s => s.BillNumberPrefix).IsRequired().HasMaxLength(10);
        builder.Property(s => s.PurchaseOrderNumberPrefix).IsRequired().HasMaxLength(10);
        builder.Property(s => s.InvoiceFooter).HasMaxLength(1000);
        builder.Property(s => s.PaymentInstructions).HasMaxLength(1000);
        builder.Property(s => s.UpdatedAtUtc).IsRequired();

        builder.OwnsOne(s => s.DefaultTaxRate, rate =>
        {
            rate.Property(r => r.Percent)
                .HasColumnName("default_tax_percent")
                .HasColumnType("numeric(5,2)")
                .IsRequired();
        });
        builder.Navigation(s => s.DefaultTaxRate).IsRequired();

        builder.Ignore(s => s.DomainEvents);
    }
}
