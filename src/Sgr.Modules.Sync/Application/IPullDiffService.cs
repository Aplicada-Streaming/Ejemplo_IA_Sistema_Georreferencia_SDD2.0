using Microsoft.EntityFrameworkCore;
using Sgr.Domain.Audit;
using Sgr.Persistence;

namespace Sgr.Modules.Sync.Application;

public interface IPullDiffService
{
    /// <summary>
    /// Devuelve los <see cref="AuditEvent"/> del survey posteriores a <paramref name="since"/>
    /// (o todos si <paramref name="since"/> es null).
    ///
    /// El orden es por <c>TimestampOriginal ASC</c> + <c>AppliedAt ASC</c> para garantizar
    /// determinismo y permitir al cliente aplicar los eventos en el mismo orden que el backend.
    ///
    /// Cobertura del survey: el método busca en
    ///   - Survey directamente (entityType=survey, entityId=surveyId)
    ///   - Points pertenecientes a ese survey (entityType=point, entityId in pointIds)
    ///   - Photos de esos points (entityType=photo) — no soportado en Slice 1
    /// </summary>
    Task<PullDiffResponse> PullAsync(Guid surveyId, DateTime? since, int maxBatchSize, CancellationToken ct = default);
}

public sealed class PullDiffService : IPullDiffService
{
    private readonly SgrDbContext _db;

    public PullDiffService(SgrDbContext db) => _db = db;

    public async Task<PullDiffResponse> PullAsync(
        Guid surveyId,
        DateTime? since,
        int maxBatchSize,
        CancellationToken ct = default)
    {
        if (surveyId == Guid.Empty)
            throw new ArgumentException("surveyId is required.", nameof(surveyId));
        if (maxBatchSize is < 1 or > 1000)
            throw new ArgumentException("maxBatchSize must be in [1, 1000].", nameof(maxBatchSize));

        // Verificar que el survey existe.
        var surveyExists = await _db.Surveys.AsNoTracking().AnyAsync(s => s.Id == surveyId, ct);
        if (!surveyExists) throw new KeyNotFoundException($"Survey {surveyId} not found.");

        // Conjunto de Points del survey (incluye soft-deleted: el cliente debe enterarse de las eliminaciones).
        var pointIdsQuery = _db.Points.AsNoTracking()
            .Where(p => p.SurveyId == surveyId)
            .Select(p => p.Id);

        var query = _db.AuditEvents.AsNoTracking()
            .Where(a =>
                (a.EntityType == AuditEntityType.Survey && a.EntityId == surveyId) ||
                (a.EntityType == AuditEntityType.Point && pointIdsQuery.Contains(a.EntityId)));
        if (since is not null)
            query = query.Where(a => a.AppliedAt > since.Value);

        // Ordenar primero por AppliedAt (cuándo se materializó en el server) para que el cliente
        // pueda usar AppliedAt del último evento como nuevo `since` y nunca reciba el mismo dos veces.
        query = query
            .OrderBy(a => a.AppliedAt)
            .ThenBy(a => a.TimestampOriginal)
            .ThenBy(a => a.Id);

        var batch = await query.Take(maxBatchSize + 1).ToListAsync(ct);
        var hasMore = batch.Count > maxBatchSize;
        if (hasMore) batch.RemoveAt(batch.Count - 1);

        var items = batch.Select(a => new PullDiffItem(
            EventId: a.Id,
            EntityType: a.EntityType,
            EntityId: a.EntityId,
            EventType: a.EventType,
            Field: a.FieldKey,
            OldValueJson: a.OldValueJson,
            NewValueJson: a.NewValueJson,
            AuthorId: a.AuthorId,
            Origin: a.Origin,
            DeviceId: a.DeviceId,
            TimestampOriginal: a.TimestampOriginal,
            AppliedAt: a.AppliedAt)).ToList();

        var nextSince = items.Count > 0 ? items[^1].AppliedAt : since;

        return new PullDiffResponse(items, nextSince, hasMore);
    }
}

public sealed record PullDiffItem(
    Guid EventId,
    string EntityType,
    Guid EntityId,
    string EventType,
    string? Field,
    string? OldValueJson,
    string? NewValueJson,
    Guid AuthorId,
    string Origin,
    string? DeviceId,
    DateTime TimestampOriginal,
    DateTime AppliedAt);

public sealed record PullDiffResponse(
    IReadOnlyList<PullDiffItem> Items,
    DateTime? NextSince,
    bool HasMore);
