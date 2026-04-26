using Microsoft.EntityFrameworkCore;
using Sgr.Domain.Templates;
using Sgr.Persistence;

namespace Sgr.Modules.Templates.Application;

public interface ITemplateVersionQuery
{
    Task<TemplateVersionDto?> FindAsync(Guid id, CancellationToken ct = default);
    Task<TemplateVersionDto?> FindRootPublishedAsync(CancellationToken ct = default);
    Task<bool> ExistsPublishedAsync(Guid id, CancellationToken ct = default);
}

public sealed class TemplateVersionQuery : ITemplateVersionQuery
{
    private readonly SgrDbContext _db;

    public TemplateVersionQuery(SgrDbContext db) => _db = db;

    public async Task<TemplateVersionDto?> FindAsync(Guid id, CancellationToken ct = default)
    {
        var row = await (
            from v in _db.TemplateVersions.AsNoTracking()
            join t in _db.Templates.AsNoTracking() on v.TemplateId equals t.Id
            where v.Id == id
            select new TemplateVersionDto(v.Id, t.Id, t.Name, v.VersionNumber, v.Status, v.PublishedAt)
        ).FirstOrDefaultAsync(ct);
        return row;
    }

    public async Task<TemplateVersionDto?> FindRootPublishedAsync(CancellationToken ct = default)
    {
        var row = await (
            from v in _db.TemplateVersions.AsNoTracking()
            join t in _db.Templates.AsNoTracking() on v.TemplateId equals t.Id
            where t.IsRoot && v.Status == TemplateVersionStatus.Publicada
            orderby v.VersionNumber descending
            select new TemplateVersionDto(v.Id, t.Id, t.Name, v.VersionNumber, v.Status, v.PublishedAt)
        ).FirstOrDefaultAsync(ct);
        return row;
    }

    public Task<bool> ExistsPublishedAsync(Guid id, CancellationToken ct = default) =>
        _db.TemplateVersions
            .AsNoTracking()
            .AnyAsync(v => v.Id == id && v.Status == TemplateVersionStatus.Publicada, ct);
}
