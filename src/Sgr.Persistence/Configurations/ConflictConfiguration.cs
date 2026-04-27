using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Sgr.Domain.Conflicts;

namespace Sgr.Persistence.Configurations;

public sealed class ConflictConfiguration : IEntityTypeConfiguration<Conflict>
{
    public void Configure(EntityTypeBuilder<Conflict> builder)
    {
        builder.ToTable("Conflicts", t =>
        {
            t.HasCheckConstraint("CK_Conflicts_Type",
                "[Type] IN ('lww','owner_precedence','post_close')");
            t.HasCheckConstraint("CK_Conflicts_Status",
                "[Status] IN ('pendiente','resuelto_revertido','resuelto_sin_cambio')");
        });
        builder.HasKey(x => x.Id);

        builder.Property(x => x.SurveyId).IsRequired();
        builder.Property(x => x.Type).HasMaxLength(32).IsRequired();
        builder.Property(x => x.EventId).IsRequired();
        builder.Property(x => x.PointId);
        builder.Property(x => x.FieldKey).HasMaxLength(128);
        builder.Property(x => x.AuthorId).IsRequired();
        builder.Property(x => x.AttemptedValueJson);
        builder.Property(x => x.CurrentValueJson);
        builder.Property(x => x.AttemptedAtUtc).IsRequired();
        builder.Property(x => x.Status).HasMaxLength(32).IsRequired();
        builder.Property(x => x.ResolvedAtUtc);
        builder.Property(x => x.ResolvedBy);
        builder.Property(x => x.ResolutionNote).HasMaxLength(2000);
        builder.Property(x => x.CreatedAt).IsRequired();

        builder.HasIndex(x => x.SurveyId);
        builder.HasIndex(x => new { x.SurveyId, x.Status });
        builder.HasIndex(x => x.EventId);
    }
}
