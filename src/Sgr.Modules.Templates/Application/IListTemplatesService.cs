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

        var byTemplate = publishedVersions.ToDictionary(v => v.TemplateId);

        return templates.Select(t =>
        {
            byTemplate.TryGetValue(t.Id, out var v);
            return new TemplateSummaryDto(
                Id: t.Id,
                Name: t.Name,
                IsRoot: t.IsRoot,
                ParentTemplateId: t.ParentTemplateId,
                LatestPublishedVersionId: v?.Id,
                LatestPublishedVersionNumber: v?.VersionNumber,
                LatestPublishedAt: v?.PublishedAt,
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
    DateTime CreatedAt);
