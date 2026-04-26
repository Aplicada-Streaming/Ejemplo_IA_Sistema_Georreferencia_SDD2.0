using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Sgr.Domain.Audit;

namespace Sgr.Persistence.Configurations;

/// <summary>
/// Audit events are append-only at the DB level (RN-10).
/// The migration that introduces this table also creates a trigger blocking UPDATE/DELETE.
/// </summary>
public sealed class AuditEventConfiguration : IEntityTypeConfiguration<AuditEvent>
{
    public void Configure(EntityTypeBuilder<AuditEvent> builder)
    {
        builder.ToTable("AuditEvents", t =>
        {
            t.HasCheckConstraint(
                "CK_AuditEvents_EntityType",
                "[EntityType] IN ('survey','point','photo')");
            t.HasCheckConstraint(
                "CK_AuditEvents_EventType",
                "[EventType] IN ('created','field_updated','deleted','restored','merged')");
            t.HasCheckConstraint(
                "CK_AuditEvents_Origin",
                "[Origin] IN ('mobile_capture','mobile_edit','web_edit','web_manual_upload')");
        });
        builder.HasKey(x => x.Id);

        builder.Property(x => x.EntityType).HasMaxLength(16).IsRequired();
        builder.Property(x => x.EntityId).IsRequired();
        builder.Property(x => x.EventType).HasMaxLength(32).IsRequired();
        builder.Property(x => x.FieldKey).HasMaxLength(100);
        builder.Property(x => x.OldValueJson);
        builder.Property(x => x.NewValueJson);
        builder.Property(x => x.AuthorId).IsRequired();
        builder.Property(x => x.Origin).HasMaxLength(32).IsRequired();
        builder.Property(x => x.DeviceId).HasMaxLength(64);
        builder.Property(x => x.TimestampOriginal).IsRequired();
        builder.Property(x => x.AppliedAt).IsRequired();

        builder.HasIndex(x => new { x.EntityType, x.EntityId, x.AppliedAt });
        builder.HasIndex(x => x.AuthorId);
        builder.HasIndex(x => x.TimestampOriginal);
    }
}
