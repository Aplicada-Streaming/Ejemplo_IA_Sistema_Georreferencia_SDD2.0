using Microsoft.EntityFrameworkCore;
using Sgr.Domain.Common;
using Sgr.Domain.Templates;
using Sgr.Persistence;

namespace Sgr.Modules.Templates.Application;

/// <summary>
/// Servicio que orquesta el ciclo de vida editor de plantillas (E.5.c):
/// crear plantilla hija → editar draft → publicar.
///
/// Reglas de dominio enforced acá:
///   • Plantilla raíz no se modifica (RN-03 — sólo admin puede crear hijas).
///   • Versiones publicadas son inmutables (RN-05) — los métodos Update lanzan
///     InvalidOperationException si la versión ya está publicada.
///   • Cada plantilla nueva nace con una v1 borrador clonada de la raíz publicada
///     más reciente, así el admin no arranca con un schema vacío.
/// </summary>
public interface ITemplateEditorService
{
    /// <summary>
    /// Crea una plantilla nueva (hija de raíz) + v1 borrador clonada del último
    /// publicado de la raíz. Devuelve el versionId del nuevo borrador.
    /// </summary>
    Task<TemplateCreatedDto> CreateTemplateAsync(string name, CancellationToken ct = default);

    /// <summary>Actualiza fields de una versión en borrador. Lanza si está publicada.</summary>
    Task UpdateFieldsAsync(Guid versionId, string fieldDefinitionsJson, CancellationToken ct = default);

    /// <summary>Actualiza captureParams de una versión en borrador.</summary>
    Task UpdateCaptureParamsAsync(Guid versionId, string captureParamsJson, CancellationToken ct = default);

    /// <summary>Publica un borrador. Idempotente si ya está publicado: no rehace.</summary>
    Task PublishAsync(Guid versionId, CancellationToken ct = default);
}

public sealed record TemplateCreatedDto(Guid TemplateId, Guid DraftVersionId);

public sealed class TemplateEditorService : ITemplateEditorService
{
    private readonly SgrDbContext _db;
    private readonly IDateTimeProvider _clock;

    public TemplateEditorService(SgrDbContext db, IDateTimeProvider clock)
    {
        _db = db;
        _clock = clock;
    }

    public async Task<TemplateCreatedDto> CreateTemplateAsync(string name, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("El nombre es requerido.", nameof(name));

        var root = await _db.Templates.AsNoTracking()
            .FirstOrDefaultAsync(t => t.IsRoot, ct)
            ?? throw new InvalidOperationException("No existe la plantilla raíz seedeada.");

        // Clonamos campos + capture params de la última versión publicada de la raíz
        // para que el admin tenga un schema sano de partida.
        var rootLatest = await _db.TemplateVersions.AsNoTracking()
            .Where(v => v.TemplateId == root.Id && v.Status == TemplateVersionStatus.Publicada)
            .OrderByDescending(v => v.VersionNumber)
            .FirstOrDefaultAsync(ct)
            ?? throw new InvalidOperationException("La plantilla raíz no tiene versiones publicadas para clonar.");

        var now = _clock.UtcNow;
        var template = Template.CreateChild(Guid.NewGuid(), name, root.Id, now);
        _db.Templates.Add(template);

        var draft = TemplateVersion.CreateDraft(
            id: Guid.NewGuid(),
            templateId: template.Id,
            versionNumber: 1,
            fieldDefinitionsJson: rootLatest.FieldDefinitionsJson,
            captureParamsJson: rootLatest.CaptureParamsJson,
            createdAt: now);
        _db.TemplateVersions.Add(draft);

        await _db.SaveChangesAsync(ct);
        return new TemplateCreatedDto(template.Id, draft.Id);
    }

    public async Task UpdateFieldsAsync(Guid versionId, string fieldDefinitionsJson, CancellationToken ct = default)
    {
        var version = await LoadVersionAsync(versionId, ct);
        version.UpdateFieldDefinitions(fieldDefinitionsJson);
        await _db.SaveChangesAsync(ct);
    }

    public async Task UpdateCaptureParamsAsync(Guid versionId, string captureParamsJson, CancellationToken ct = default)
    {
        var version = await LoadVersionAsync(versionId, ct);
        version.UpdateCaptureParams(captureParamsJson);
        await _db.SaveChangesAsync(ct);
    }

    public async Task PublishAsync(Guid versionId, CancellationToken ct = default)
    {
        var version = await LoadVersionAsync(versionId, ct);
        if (version.IsPublished) return;
        version.Publish(_clock.UtcNow);
        await _db.SaveChangesAsync(ct);
    }

    private async Task<TemplateVersion> LoadVersionAsync(Guid versionId, CancellationToken ct)
    {
        var version = await _db.TemplateVersions.FirstOrDefaultAsync(v => v.Id == versionId, ct)
            ?? throw new TemplateNotFoundException($"TemplateVersion {versionId} no existe.");
        return version;
    }
}
