using Microsoft.EntityFrameworkCore;
using Sgr.Domain.MergeCandidates;
using Sgr.Domain.Points;
using Sgr.Persistence;

namespace Sgr.Modules.Sync.Application;

/// <summary>
/// US-21 / RN-09 — Detección de candidatos a fusión durante el push.
///
/// Cuando llega un Punto nuevo (o se mueven sus coords), busca otros Puntos del
/// mismo survey que cumplen TODAS las condiciones:
/// - Distintos IDs.
/// - Creados por <b>colaboradores distintos</b> (descarta duplicados del mismo creador).
/// - Distancia geodésica ≤ <c>merge_radius_m</c> de la plantilla (default 10m).
/// - Diferencia temporal ≤ <c>merge_time_window_hours</c> (default 24h).
/// - El par no está marcado <c>mantenido_separado</c> previamente (RN-09 bullet final).
///
/// El candidato queda <c>pendiente</c>; la decisión es manual (CU-11).
/// </summary>
public interface IMergeCandidateDetector
{
    /// <summary>
    /// Detecta candidatos para el Punto recién agregado al ChangeTracker. NO hace
    /// SaveChanges — el applier lo hace al final del batch para mantener atomicidad.
    /// </summary>
    Task DetectFromAsync(Point newOrMovedPoint, CancellationToken ct = default);
}

public sealed class MergeCandidateDetector : IMergeCandidateDetector
{
    private readonly SgrDbContext _db;

    public MergeCandidateDetector(SgrDbContext db) => _db = db;

    public async Task DetectFromAsync(Point newOrMovedPoint, CancellationToken ct = default)
    {
        var (radiusM, timeWindowHours) = await ResolveMergeParamsAsync(newOrMovedPoint.SurveyId, ct);

        var windowStart = newOrMovedPoint.CreatedAt.AddHours(-timeWindowHours);
        var windowEnd = newOrMovedPoint.CreatedAt.AddHours(timeWindowHours);

        // Filtro por bounding box aproximado para no traer toda la tabla — luego verificamos
        // la distancia geodésica exacta (Haversine) en memoria. 1° de lat ≈ 111km; usamos
        // radius/111000 como buffer en grados, y para lng dividimos por cos(lat) (aprox).
        var bufferDegLat = (double)radiusM / 111_000d;
        // Latitude de referencia para escalar la longitud — usamos la del punto nuevo.
        var refLat = (double)newOrMovedPoint.Latitude;
        var bufferDegLng = bufferDegLat / Math.Max(0.01, Math.Cos(refLat * Math.PI / 180));

        var minLat = newOrMovedPoint.Latitude - (decimal)bufferDegLat;
        var maxLat = newOrMovedPoint.Latitude + (decimal)bufferDegLat;
        var minLng = newOrMovedPoint.Longitude - (decimal)bufferDegLng;
        var maxLng = newOrMovedPoint.Longitude + (decimal)bufferDegLng;

        var nearby = await _db.Points.AsNoTracking()
            .Where(p => p.SurveyId == newOrMovedPoint.SurveyId
                     && p.Id != newOrMovedPoint.Id
                     && !p.IsDeleted
                     && p.CreatedBy != newOrMovedPoint.CreatedBy
                     && p.CreatedAt >= windowStart && p.CreatedAt <= windowEnd
                     && p.Latitude >= minLat && p.Latitude <= maxLat
                     && p.Longitude >= minLng && p.Longitude <= maxLng)
            .ToListAsync(ct);

        if (nearby.Count == 0) return;

        // Pares ya conocidos para evitar insertar duplicados (RN-06 idempotencia detector).
        var pointIds = nearby.Select(p => p.Id).Append(newOrMovedPoint.Id).ToHashSet();
        var existingPairs = await _db.MergeCandidates.AsNoTracking()
            .Where(m => pointIds.Contains(m.PointAId) && pointIds.Contains(m.PointBId))
            .Select(m => new { m.PointAId, m.PointBId, m.Status })
            .ToListAsync(ct);

        var existingByPair = existingPairs
            .ToDictionary(x => (x.PointAId, x.PointBId), x => x.Status);

        foreach (var other in nearby)
        {
            var dist = (decimal)HaversineMeters(
                newOrMovedPoint.Latitude, newOrMovedPoint.Longitude,
                other.Latitude, other.Longitude);
            if (dist > radiusM) continue;

            // Normalizar el par (PointAId < PointBId).
            var (pa, pb) = newOrMovedPoint.Id.CompareTo(other.Id) < 0
                ? (newOrMovedPoint, other)
                : (other, newOrMovedPoint);

            if (existingByPair.ContainsKey((pa.Id, pb.Id)))
                continue; // ya existe (en cualquier estado: pendiente, fusionado o mantenido_separado)

            _db.MergeCandidates.Add(MergeCandidate.Create(
                id: Guid.NewGuid(),
                surveyId: newOrMovedPoint.SurveyId,
                pointAId: pa.Id,
                pointBId: pb.Id,
                pointACreatedBy: pa.CreatedBy,
                pointBCreatedBy: pb.CreatedBy,
                pointACreatedAt: pa.CreatedAt,
                pointBCreatedAt: pb.CreatedAt,
                distanceMeters: Math.Round(dist, 2),
                nowUtc: DateTime.UtcNow));
        }
    }

    private async Task<(decimal radiusM, int timeWindowHours)> ResolveMergeParamsAsync(
        Guid surveyId, CancellationToken ct)
    {
        try
        {
            var versionId = await _db.Surveys.AsNoTracking()
                .Where(s => s.Id == surveyId)
                .Select(s => s.TemplateVersionId)
                .FirstOrDefaultAsync(ct);
            if (versionId == Guid.Empty) return (10m, 24);

            var json = await _db.TemplateVersions.AsNoTracking()
                .Where(v => v.Id == versionId)
                .Select(v => v.CaptureParamsJson)
                .FirstOrDefaultAsync(ct);
            if (string.IsNullOrWhiteSpace(json)) return (10m, 24);

            using var doc = System.Text.Json.JsonDocument.Parse(json);
            var radius = doc.RootElement.TryGetProperty("merge_radius_m", out var r) && r.TryGetDecimal(out var rv)
                ? rv : 10m;
            var hours = doc.RootElement.TryGetProperty("merge_time_window_hours", out var h) && h.TryGetInt32(out var hv)
                ? hv : 24;
            return (radius, hours);
        }
        catch { return (10m, 24); }
    }

    private static double HaversineMeters(decimal lat1d, decimal lng1d, decimal lat2d, decimal lng2d)
    {
        var lat1 = (double)lat1d;
        var lng1 = (double)lng1d;
        var lat2 = (double)lat2d;
        var lng2 = (double)lng2d;
        const double r = 6_371_000;
        var dLat = (lat2 - lat1) * Math.PI / 180;
        var dLng = (lng2 - lng1) * Math.PI / 180;
        var a = Math.Pow(Math.Sin(dLat / 2), 2) +
                Math.Cos(lat1 * Math.PI / 180) * Math.Cos(lat2 * Math.PI / 180) *
                Math.Pow(Math.Sin(dLng / 2), 2);
        return r * 2 * Math.Asin(Math.Sqrt(a));
    }
}
