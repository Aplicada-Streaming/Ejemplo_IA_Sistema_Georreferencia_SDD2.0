using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Sgr.Domain.SystemConfig;

namespace Sgr.Persistence.Configurations;

public sealed class SystemConfigEntryConfiguration : IEntityTypeConfiguration<SystemConfigEntry>
{
    public void Configure(EntityTypeBuilder<SystemConfigEntry> builder)
    {
        builder.ToTable("SystemConfig");
        builder.HasKey(x => x.Id);

        builder.HasIndex(x => x.Key).IsUnique();
        builder.Property(x => x.Key).HasMaxLength(128).IsRequired();
        builder.Property(x => x.ValueJson).IsRequired();
        builder.Property(x => x.UpdatedBy).IsRequired();
        builder.Property(x => x.UpdatedAt).IsRequired();
        builder.Property(x => x.CreatedAt).IsRequired();
    }
}
