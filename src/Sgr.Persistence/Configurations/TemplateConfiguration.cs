using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Sgr.Domain.Templates;

namespace Sgr.Persistence.Configurations;

public sealed class TemplateConfiguration : IEntityTypeConfiguration<Template>
{
    public void Configure(EntityTypeBuilder<Template> builder)
    {
        builder.ToTable("Templates");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Name).HasMaxLength(150).IsRequired();
        builder.Property(x => x.ParentTemplateId);
        builder.Property(x => x.IsRoot).IsRequired();
        builder.Property(x => x.IsDeletable).IsRequired();
        builder.Property(x => x.CreatedAt).IsRequired();

        // RN-03: only one root template can exist (filtered unique index).
        builder.HasIndex(x => x.IsRoot)
            .HasFilter("[IsRoot] = 1")
            .IsUnique();

        builder.HasIndex(x => x.ParentTemplateId);
    }
}

public sealed class TemplateVersionConfiguration : IEntityTypeConfiguration<TemplateVersion>
{
    public void Configure(EntityTypeBuilder<TemplateVersion> builder)
    {
        builder.ToTable("TemplateVersions", t =>
        {
            t.HasCheckConstraint(
                "CK_TemplateVersions_Status",
                "[Status] IN ('borrador','publicada')");
        });
        builder.HasKey(x => x.Id);

        builder.Property(x => x.TemplateId).IsRequired();
        builder.Property(x => x.VersionNumber).IsRequired();
        builder.Property(x => x.Status).HasMaxLength(16).IsRequired();
        builder.Property(x => x.FieldDefinitionsJson).IsRequired();
        builder.Property(x => x.CaptureParamsJson).IsRequired();
        builder.Property(x => x.PublishedAt);
        builder.Property(x => x.CreatedAt).IsRequired();

        builder.HasIndex(x => new { x.TemplateId, x.VersionNumber }).IsUnique();
    }
}
