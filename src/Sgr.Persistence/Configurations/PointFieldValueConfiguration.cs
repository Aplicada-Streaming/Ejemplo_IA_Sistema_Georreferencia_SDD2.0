using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Sgr.Domain.Points;

namespace Sgr.Persistence.Configurations;

public sealed class PointFieldValueConfiguration : IEntityTypeConfiguration<PointFieldValue>
{
    public void Configure(EntityTypeBuilder<PointFieldValue> builder)
    {
        builder.ToTable("PointFieldValues");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.PointId).IsRequired();
        builder.Property(x => x.FieldKey).HasMaxLength(100).IsRequired();
        builder.Property(x => x.ValueJson).HasColumnType("nvarchar(max)");
        builder.Property(x => x.UpdatedBy).IsRequired();
        builder.Property(x => x.UpdatedAt).IsRequired();
        builder.Property(x => x.CreatedAt).IsRequired();

        // Una fila por (PointId, FieldKey). El upsert se modela manualmente
        // en IEventApplier según LWW + RN-07.
        builder.HasIndex(x => new { x.PointId, x.FieldKey })
            .IsUnique()
            .HasDatabaseName("UQ_PointFieldValues_PointKey");

        builder.HasIndex(x => x.PointId);
    }
}
