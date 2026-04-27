using Microsoft.EntityFrameworkCore;
using Sgr.Domain.Audit;
using Sgr.Domain.Common;
using Sgr.Domain.MergeCandidates;
using Sgr.Modules.Surveys.Application;
using Sgr.Persistence;

namespace Sgr.Modules.Sync.Application;

/// <summary>
/// Operaciones sobre candidatos a fusión (US-21 / US-22 / CU-11).
///
/// <c>ListAsync</c> filtra por survey y status (default: pendientes).
///
/// <c>KeepSeparateAsync</c> marca el par como <c>mantenido_separado</c> — el detector
/// no volverá a proponerlo (RN-09).
///
/// <c>MergeAsync</c> consolida ambos puntos en uno (US-22):
/// - <c>keep_a</c>: el Punto resultante es A (su posición y datos prevalecen).
/// - <c>keep_b</c>: idem con B.
/// - <c>centroid</c>: A queda como resultante con coords del punto medio entre A y B.
/// En todos los casos las fotos del Punto descartado pasan al resultante,
/// el descartado queda <c>IsDeleted=true</c> y se registra un AuditEvent <c>merged</c>.
/// </summary>
public interface IMergeCandidatesService
{
    Task<IReadOnlyList<MergeCandidateDto>> ListAsync(
        Guid? surveyId, string? status, CurrentUserContext actor, CancellationToken ct = default);

    Task<MergeCandidateDto> KeepSeparateAsync(
        Guid candidateId, CurrentUserContext actor, CancellationToken ct = default);

    Task<MergeCandidateDto> MergeAsync(
        Guid candidateId, string strategy, CurrentUserContext actor, CancellationToken ct = default);
}

public sealed record MergeCandidateDto(
    Guid Id,
    Guid SurveyId,
    Guid PointAId,
    Guid PointBId,
    Guid PointACreatedBy,
    Guid PointBCreatedBy,
    DateTime PointACreatedAt,
    DateTime PointBCreatedAt,
    decimal DistanceMeters,
    string Status,
    DateTime? ResolvedAtUtc,
    Guid? ResolvedBy,
    Guid? ResultPointId,
    string? ResolutionStrategy,
    DateTime CreatedAt);

public sealed class MergeCandidatesService : IMergeCandidatesService
{
    private readonly SgrDbContext _db;
    private readonly IDateTimeProvider _clock;

    public MergeCandidatesService(SgrDbContext db, IDateTimeProvider clock)
    {
        _db = db;
        _clock = clock;
    }

    public async Task<IReadOnlyList<MergeCandidateDto>> ListAsync(
        Guid? surveyId, string? status, CurrentUserContext actor, CancellationToken ct = default)
    {
        var query = _db.MergeCandidates.AsNoTracking().AsQueryable();
        if (surveyId is not null) query = query.Where(c => c.SurveyId == surveyId);
        var effectiveStatus = string.IsNullOrWhiteSpace(status) ? MergeCandidateStatus.Pendiente : status;
        if (effectiveStatus != "all")
            query = query.Where(c => c.Status == effectiveStatus);

        var rows = await query.OrderByDescending(c => c.CreatedAt).ToListAsync(ct);
        return rows.Select(ToDto).ToList();
    }

    public async Task<MergeCandidateDto> KeepSeparateAsync(
        Guid candidateId, CurrentUserContext actor, CancellationToken ct = default)
    {
        var candidate = await _db.MergeCandidates.FirstOrDefaultAsync(c => c.Id == candidateId, ct)
            ?? throw new InvalidOperationException($"Candidate {candidateId} no existe.");
        candidate.MarkKeptSeparate(actor.UserId, _clock.UtcNow);
        await _db.SaveChangesAsync(ct);
        return ToDto(candidate);
    }

    public async Task<MergeCandidateDto> MergeAsync(
        Guid candidateId, string strategy, CurrentUserContext actor, CancellationToken ct = default)
    {
        if (!MergeStrategies.IsValid(strategy))
            throw new ArgumentException($"Strategy '{strategy}' inválida.");

        var candidate = await _db.MergeCandidates.FirstOrDefaultAsync(c => c.Id == candidateId, ct)
            ?? throw new InvalidOperationException($"Candidate {candidateId} no existe.");
        if (candidate.Status != MergeCandidateStatus.Pendiente)
            throw new InvalidOperationException("El candidato ya estaba resuelto.");

        var pointA = await _db.Points.FirstOrDefaultAsync(p => p.Id == candidate.PointAId, ct)
            ?? throw new InvalidOperationException($"Point A {candidate.PointAId} no existe.");
        var pointB = await _db.Points.FirstOrDefaultAsync(p => p.Id == candidate.PointBId, ct)
            ?? throw new InvalidOperationException($"Point B {candidate.PointBId} no existe.");

        var (kept, dropped) = strategy switch
        {
            MergeStrategies.KeepB => (pointB, pointA),
            _ => (pointA, pointB), // centroid y keep_a → resultante es A
        };

        var now = _clock.UtcNow;

        if (strategy == MergeStrategies.Centroid)
        {
            var avgLat = (pointA.Latitude + pointB.Latitude) / 2m;
            var avgLng = (pointA.Longitude + pointB.Longitude) / 2m;
            kept.UpdateCoords(avgLat, avgLng, kept.AccuracyM, now);
        }

        // Mover fotos del dropped al kept.
        var droppedPhotos = await _db.Photos
            .Where(p => p.PointId == dropped.Id && !p.IsDeleted)
            .ToListAsync(ct);
        foreach (var photo in droppedPhotos)
        {
            // Photo no tiene un setter público de PointId — usamos reflection para mover.
            // Alternativa más limpia: agregar Photo.ReassignTo(...) al dominio. Pendiente.
            typeof(Sgr.Domain.Photos.Photo).GetProperty(nameof(Sgr.Domain.Photos.Photo.PointId))!
                .SetValue(photo, (Guid?)kept.Id);
        }

        // Mover field values: si kept ya tiene el campo, se queda con el suyo (heredado priority).
        var droppedFieldValues = await _db.PointFieldValues
            .Where(v => v.PointId == dropped.Id)
            .ToListAsync(ct);
        var keptFieldKeys = (await _db.PointFieldValues.AsNoTracking()
            .Where(v => v.PointId == kept.Id)
            .Select(v => v.FieldKey)
            .ToListAsync(ct)).ToHashSet();
        foreach (var v in droppedFieldValues)
        {
            if (keptFieldKeys.Contains(v.FieldKey))
            {
                _db.PointFieldValues.Remove(v); // duplicate — drop
            }
            else
            {
                typeof(Sgr.Domain.Points.PointFieldValue).GetProperty(nameof(Sgr.Domain.Points.PointFieldValue.PointId))!
                    .SetValue(v, kept.Id);
            }
        }

        // Soft-delete del dropped.
        dropped.SoftDelete(now);

        // Audit RN-10.
        _db.AuditEvents.Add(AuditEvent.Create(
            id: Guid.NewGuid(),
            entityType: AuditEntityType.Point,
            entityId: kept.Id,
            eventType: AuditEventType.Merged,
            fieldKey: null,
            oldValueJson: System.Text.Json.JsonSerializer.Serialize(new
            {
                droppedPointId = dropped.Id,
                strategy,
                droppedLat = dropped.Latitude,
                droppedLng = dropped.Longitude,
            }),
            newValueJson: System.Text.Json.JsonSerializer.Serialize(new
            {
                resultPointId = kept.Id,
                strategy,
                lat = kept.Latitude,
                lng = kept.Longitude,
                photosMoved = droppedPhotos.Count,
            }),
            authorId: actor.UserId,
            origin: AuditOrigin.WebEdit,
            deviceId: null,
            timestampOriginal: now,
            appliedAt: now));

        candidate.MarkMerged(kept.Id, strategy, actor.UserId, now);
        await _db.SaveChangesAsync(ct);
        return ToDto(candidate);
    }

    private static MergeCandidateDto ToDto(MergeCandidate c) => new(
        c.Id, c.SurveyId, c.PointAId, c.PointBId,
        c.PointACreatedBy, c.PointBCreatedBy, c.PointACreatedAt, c.PointBCreatedAt,
        c.DistanceMeters, c.Status, c.ResolvedAtUtc, c.ResolvedBy,
        c.ResultPointId, c.ResolutionStrategy, c.CreatedAt);
}
