using Microsoft.EntityFrameworkCore;
using Sgr.Persistence;

namespace Sgr.Modules.Templates.Application;

public interface IListTemplatesService
{
    /// <summary>
    /// Lista todas las plantillas con su versión publicada más reciente (si existe).
    /// El primer item siempre es la plantilla raíz (RN-03).
    /// </summary>
    Task<IReadOnlyList<TemplateSummaryDto>> ListAsync(CancellationToken ct = default);
}

public sealed class ListTemplatesService : IListTemplatesService
{
    private readonly SgrDbContext _db;

    public ListTemplatesService(SgrDbContext db) => _db = db;

    public async Task<IReadOnlyList<TemplateSummaryDto>> ListAsync(CancellationToken ct = default)
    {
        // Plantillas + última versión publicada por cada una. Hacemos dos queries y combinamos
        // en memoria; el set de plantillas en este sistema es pequeño (1 raíz + algunas hijas).
        var templates = await _db.Templates.AsNoTracking()
            .OrderByDescending(t => t.IsRoot)
            .ThenBy(t => t.Name)
            .ToListAsync(ct);

        var publishedVersions = await _db.TemplateVersions.AsNoTracking()
            .Where(v => v.Status == "publicada")
            .GroupBy(v => v.TemplateId)
            .Select(g => g.OrderByDescending(v => v.VersionNumber).First())
            .ToListAsync(ct);

        // E.5.c: también traemos el último draft (independiente de si hay publicado)
        // para que el admin pueda volver a una plantilla recién creada vía la lista.
        var draftVersions = await _db.TemplateVersions.AsNoTracking()
            .Where(v => v.Status == "borrador")
            .GroupBy(v => v.TemplateId)
            .Select(g => g.OrderByDescending(v => v.VersionNumber).First())
            .ToListAsync(ct);

        var publishedByTemplate = publishedVersions.ToDictionary(v => v.TemplateId);
        var draftByTemplate = draftVersions.ToDictionary(v => v.TemplateId);

        return templates.Select(t =>
        {
            publishedByTemplate.TryGetValue(t.Id, out var pub);
            draftByTemplate.TryGetValue(t.Id, out var draft);
            return new TemplateSummaryDto(
                Id: t.Id,
                Name: t.Name,
                IsRoot: t.IsRoot,
                ParentTemplateId: t.ParentTemplateId,
                LatestPublishedVersionId: pub?.Id,
                LatestPublishedVersionNumber: pub?.VersionNumber,
                LatestPublishedAt: pub?.PublishedAt,
                LatestDraftVersionId: draft?.Id,
                LatestDraftVersionNumber: draft?.VersionNumber,
                CreatedAt: t.CreatedAt);
        }).ToList();
    }
}

public sealed record TemplateSummaryDto(
    Guid Id,
    string Name,
    bool IsRoot,
    Guid? ParentTemplateId,
    Guid? LatestPublishedVersionId,
    int? LatestPublishedVersionNumber,
    DateTime? LatestPublishedAt,
    Guid? LatestDraftVersionId,
    int? LatestDraftVersionNumber,
    DateTime CreatedAt);
