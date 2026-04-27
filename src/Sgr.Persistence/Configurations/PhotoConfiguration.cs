using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Sgr.Domain.Photos;

namespace Sgr.Persistence.Configurations;

public sealed class PhotoConfiguration : IEntityTypeConfiguration<Photo>
{
    public void Configure(EntityTypeBuilder<Photo> builder)
    {
        builder.ToTable("Photos", t =>
        {
            t.HasCheckConstraint(
                "CK_Photos_AdapterName",
                "[AdapterName] IN ('local','s3','ftp','sftp')");
            t.HasCheckConstraint(
                "CK_Photos_Origin",
                "[Origin] IN ('mobile_capture','mobile_edit','web_edit','web_manual_upload')");
            t.HasCheckConstraint("CK_Photos_SizeBytes", "[SizeBytes] > 0");
        });
        builder.HasKey(x => x.Id);

        builder.Property(x => x.PointId).IsRequired();
        builder.Property(x => x.Comment).HasMaxLength(2000);
        builder.Property(x => x.AdapterName).HasMaxLength(16).IsRequired();
        builder.Property(x => x.AdapterRef).HasMaxLength(500).IsRequired();
        builder.Property(x => x.SizeBytes).IsRequired();
        builder.Property(x => x.ContentHash).HasMaxLength(64).IsRequired();
        builder.Property(x => x.MetadataJson).IsRequired();
        builder.Property(x => x.CreatedBy).IsRequired();
        builder.Property(x => x.Origin).HasMaxLength(32).IsRequired();
        builder.Property(x => x.CreatedAt).IsRequired();
        builder.Property(x => x.IsDeleted).IsRequired();
        builder.Property(x => x.DeletedAt);

        builder.HasIndex(x => x.PointId);
        builder.HasIndex(x => x.ContentHash);
    }
}
