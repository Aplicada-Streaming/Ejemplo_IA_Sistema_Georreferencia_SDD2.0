using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Sgr.Domain.Points;

namespace Sgr.Persistence.Configurations;

public sealed class PointConfiguration : IEntityTypeConfiguration<Point>
{
    public void Configure(EntityTypeBuilder<Point> builder)
    {
        builder.ToTable("Points", t =>
        {
            t.HasCheckConstraint(
                "CK_Points_Origin",
                "[Origin] IN ('mobile_capture','mobile_edit','web_edit','web_manual_upload')");
            t.HasCheckConstraint(
                "CK_Points_CaptureMode",
                "[CaptureMode] IN ('detenido','movil','web')");
            t.HasCheckConstraint(
                "CK_Points_Latitude",
                "[Latitude] BETWEEN -90 AND 90");
            t.HasCheckConstraint(
                "CK_Points_Longitude",
                "[Longitude] BETWEEN -180 AND 180");
        });
        builder.HasKey(x => x.Id);

        builder.Property(x => x.SurveyId).IsRequired();
        builder.Property(x => x.Latitude).HasColumnType("decimal(9,6)").IsRequired();
        builder.Property(x => x.Longitude).HasColumnType("decimal(9,6)").IsRequired();
        builder.Property(x => x.AccuracyM).HasColumnType("decimal(7,2)");
        builder.Property(x => x.Title).HasMaxLength(200);
        builder.Property(x => x.Description).HasMaxLength(2000);
        builder.Property(x => x.CreatedBy).IsRequired();
        builder.Property(x => x.Origin).HasMaxLength(32).IsRequired();
        builder.Property(x => x.CaptureMode).HasMaxLength(16).IsRequired();
        builder.Property(x => x.DeviceId).HasMaxLength(64);
        builder.Property(x => x.CreatedAt).IsRequired();
        builder.Property(x => x.UpdatedAt).IsRequired();
        builder.Property(x => x.IsDeleted).IsRequired();
        builder.Property(x => x.DeletedAt);

        builder.HasIndex(x => x.SurveyId);
        builder.HasIndex(x => new { x.SurveyId, x.CreatedBy });
        builder.HasIndex(x => x.UpdatedAt);
    }
}
