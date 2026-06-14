using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NikaFitness.Domain.Sequences;

namespace NikaFitness.Infrastructure.Persistence.Configurations;

public sealed class NumberSequenceConfiguration : IEntityTypeConfiguration<NumberSequence>
{
    public void Configure(EntityTypeBuilder<NumberSequence> builder)
    {
        builder.ToTable("number_sequences");
        builder.HasKey(s => s.Id);

        builder.Property(s => s.Key).IsRequired().HasMaxLength(60);
        builder.HasIndex(s => s.Key).IsUnique();

        builder.Property(s => s.NextValue).IsRequired();
    }
}
