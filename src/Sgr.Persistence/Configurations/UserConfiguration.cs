using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Sgr.Domain.Identity;

namespace Sgr.Persistence.Configurations;

public sealed class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("Users", t =>
        {
            t.HasCheckConstraint(
                "CK_Users_Role",
                "[Role] IN ('admin_raiz','jefe_area','relevador')");
            t.HasCheckConstraint(
                "CK_Users_Status",
                "[Status] IN ('pendiente_aceptacion','activo','inhabilitado','dado_de_baja')");
        });
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Email).HasMaxLength(254).IsRequired();
        builder.HasIndex(x => x.Email).IsUnique();

        builder.Property(x => x.PasswordHash).HasMaxLength(512).IsRequired();
        builder.Property(x => x.FullName).HasMaxLength(200).IsRequired();
        builder.Property(x => x.Role).HasMaxLength(32).IsRequired();
        builder.Property(x => x.Status).HasMaxLength(32).IsRequired();

        builder.Property(x => x.AreaId);
        builder.Property(x => x.CreatedAt).IsRequired();
        builder.Property(x => x.AcceptedAt);

        builder.HasIndex(x => new { x.AreaId, x.Role, x.Status });
    }
}
