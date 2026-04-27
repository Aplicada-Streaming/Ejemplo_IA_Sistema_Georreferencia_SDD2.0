using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Sgr.Domain.MergeCandidates;

namespace Sgr.Persistence.Configurations;

public sealed class MergeCandidateConfiguration : IEntityTypeConfiguration<MergeCandidate>
{
    public void Configure(EntityTypeBuilder<MergeCandidate> builder)
    {
        builder.ToTable("MergeCandidates", t =>
        {
            t.HasCheckConstraint("CK_MergeCandidates_Status",
                "[Status] IN ('pendiente','fusionado','mantenido_separado')");
            t.HasCheckConstraint("CK_MergeCandidates_DistinctPoints",
                "[PointAId] <> [PointBId]");
        });
        builder.HasKey(x => x.Id);

        builder.Property(x => x.SurveyId).IsRequired();
        builder.Property(x => x.PointAId).IsRequired();
        builder.Property(x => x.PointBId).IsRequired();
        builder.Property(x => x.PointACreatedBy).IsRequired();
        builder.Property(x => x.PointBCreatedBy).IsRequired();
        builder.Property(x => x.PointACreatedAt).IsRequired();
        builder.Property(x => x.PointBCreatedAt).IsRequired();
        builder.Property(x => x.DistanceMeters).HasPrecision(9, 2).IsRequired();
        builder.Property(x => x.Status).HasMaxLength(32).IsRequired();
        builder.Property(x => x.ResolvedAtUtc);
        builder.Property(x => x.ResolvedBy);
        builder.Property(x => x.ResultPointId);
        builder.Property(x => x.ResolutionStrategy).HasMaxLength(16);
        builder.Property(x => x.CreatedAt).IsRequired();

        builder.HasIndex(x => x.SurveyId);
        builder.HasIndex(x => new { x.SurveyId, x.Status });
        // Único por par — garantiza no duplicar (A,B) bajo el invariante PointAId<PointBId.
        builder.HasIndex(x => new { x.PointAId, x.PointBId }).IsUnique();
    }
}
