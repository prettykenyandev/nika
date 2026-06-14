using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NikaFitness.Domain.Auditing;

namespace NikaFitness.Infrastructure.Persistence.Configurations;

public sealed class AuditLogEntryConfiguration : IEntityTypeConfiguration<AuditLogEntry>
{
    public void Configure(EntityTypeBuilder<AuditLogEntry> builder)
    {
        builder.ToTable("audit_log");
        builder.HasKey(a => a.Id);
        builder.Property(a => a.Id).ValueGeneratedNever();

        builder.Property(a => a.OccurredAtUtc).IsRequired();
        builder.Property(a => a.ActorId);
        builder.Property(a => a.ActorEmail).IsRequired().HasMaxLength(256);
        builder.Property(a => a.Action).IsRequired().HasMaxLength(120);
        builder.Property(a => a.Summary).HasMaxLength(1000);

        builder.HasIndex(a => a.OccurredAtUtc);
        builder.HasIndex(a => a.Action);

        builder.Ignore(a => a.DomainEvents);
    }
}
