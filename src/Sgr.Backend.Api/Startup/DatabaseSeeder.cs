using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Sgr.Domain.Common;
using Sgr.Domain.Identity;
using Sgr.Domain.Templates;
using Sgr.Modules.Identity.Authentication;
using Sgr.Persistence;

namespace Sgr.Backend.Api.Startup;

public sealed class SeedOptions
{
    public const string SectionName = "Seed";

    public string AdminEmail { get; set; } = "admin@vialidad.local";
    public string AdminPassword { get; set; } = "Admin1234!";
    public string AdminFullName { get; set; } = "Administrador raíz";

    public string DefaultAreaName { get; set; } = "Área Default";

    public string JefeEmail { get; set; } = "jefe@vialidad.local";
    public string JefePassword { get; set; } = "Jefe1234!";
    public string JefeFullName { get; set; } = "Jefe de Área";

    public string RelevadorEmail { get; set; } = "relevador@vialidad.local";
    public string RelevadorPassword { get; set; } = "Relev1234!";
    public string RelevadorFullName { get; set; } = "Relevador de Campo";
}

public static class DatabaseSeeder
{
    public static async Task SeedAsync(
        SgrDbContext db,
        IPasswordHasher hasher,
        IDateTimeProvider clock,
        SeedOptions seed,
        ILogger logger,
        CancellationToken ct = default)
    {
        if (db.Database.IsRelational())
            await db.Database.MigrateAsync(ct);
        else
            await db.Database.EnsureCreatedAsync(ct);

        var now = clock.UtcNow;

        // 1) Default Area
        var area = await db.Areas.FirstOrDefaultAsync(a => a.Name == seed.DefaultAreaName, ct);
        if (area is null)
        {
            area = new Area(Guid.NewGuid(), seed.DefaultAreaName, "Área inicial seed", now);
            db.Areas.Add(area);
            await db.SaveChangesAsync(ct);
            logger.LogInformation("Seeded area '{Name}' (id={Id}).", area.Name, area.Id);
        }

        // 2) Admin raíz
        if (!await db.Users.AnyAsync(u => u.Role == UserRole.AdminRaiz, ct))
        {
            var admin = User.CreateAdminRoot(
                Guid.NewGuid(),
                seed.AdminEmail,
                hasher.Hash(seed.AdminPassword),
                seed.AdminFullName,
                now);
            db.Users.Add(admin);
            await db.SaveChangesAsync(ct);
            logger.LogInformation("Seeded admin raíz '{Email}'.", admin.Email);
        }

        // 3) Jefe de área activo
        var jefeEmail = seed.JefeEmail.ToLowerInvariant();
        if (!await db.Users.AnyAsync(u => u.Email == jefeEmail, ct))
        {
            var jefe = User.RegisterPending(
                Guid.NewGuid(),
                seed.JefeEmail,
                hasher.Hash(seed.JefePassword),
                seed.JefeFullName,
                UserRole.JefeArea,
                area.Id,
                now);
            jefe.Accept(now);
            db.Users.Add(jefe);
            await db.SaveChangesAsync(ct);
            logger.LogInformation("Seeded jefe '{Email}' as activo.", jefe.Email);
        }

        // 4) Relevador activo
        var relevEmail = seed.RelevadorEmail.ToLowerInvariant();
        if (!await db.Users.AnyAsync(u => u.Email == relevEmail, ct))
        {
            var relevador = User.RegisterPending(
                Guid.NewGuid(),
                seed.RelevadorEmail,
                hasher.Hash(seed.RelevadorPassword),
                seed.RelevadorFullName,
                UserRole.Relevador,
                area.Id,
                now);
            relevador.Accept(now);
            db.Users.Add(relevador);
            await db.SaveChangesAsync(ct);
            logger.LogInformation("Seeded relevador '{Email}' as activo.", relevador.Email);
        }

        // 5) Plantilla raíz publicada (RN-03 + US-06).
        if (!await db.Templates.AnyAsync(t => t.IsRoot, ct))
        {
            var rootTemplate = Template.CreateRoot(Guid.NewGuid(), "Plantilla genérica raíz", now);
            db.Templates.Add(rootTemplate);

            var version = TemplateVersion.CreateDraft(
                id: Guid.NewGuid(),
                templateId: rootTemplate.Id,
                versionNumber: 1,
                fieldDefinitionsJson: RootTemplateFieldDefinitions,
                captureParamsJson: RootTemplateCaptureParams,
                createdAt: now);
            version.Publish(now);
            db.TemplateVersions.Add(version);

            await db.SaveChangesAsync(ct);
            logger.LogInformation(
                "Seeded plantilla raíz '{Name}' v{Version} (templateId={TemplateId}, versionId={VersionId}).",
                rootTemplate.Name, version.VersionNumber, rootTemplate.Id, version.Id);
        }
    }

    /// <summary>Common fields shared by every inspection (US-06).</summary>
    private const string RootTemplateFieldDefinitions = """
        [
          { "key": "fecha_inspeccion", "label": "Fecha de inspección", "type": "fecha", "required": true },
          { "key": "condicion_general", "label": "Condición general", "type": "selección",
            "options": ["Buena","Regular","Mala","Crítica"], "required": true },
          { "key": "observaciones", "label": "Observaciones", "type": "texto", "required": false },
          { "key": "prioridad", "label": "Prioridad", "type": "selección",
            "options": ["Alta","Media","Baja"], "required": false }
        ]
        """;

    /// <summary>Capture params with sane defaults (PROJECT-BRIEF Sec. 6.1 / 7.4).</summary>
    private const string RootTemplateCaptureParams = """
        {
          "gps_timeout_seconds": 30,
          "gps_accuracy_threshold_m": 50,
          "allow_continue_with_low_accuracy": true,
          "allow_manual_coordinates_entry_mobile": false,
          "movil_radius_m": 10,
          "photo_max_long_side_px": 2048,
          "photo_jpeg_quality": 85,
          "photo_target_format": "jpg",
          "photo_keep_original": false,
          "photo_generate_thumbnail": true,
          "photo_strip_sensitive_exif": false,
          "merge_radius_m": 10,
          "merge_time_window_hours": 24
        }
        """;
}
