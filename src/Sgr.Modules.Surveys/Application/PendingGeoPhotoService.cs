using Microsoft.EntityFrameworkCore;
using Sgr.Domain.Audit;
using Sgr.Domain.Common;
using Sgr.Domain.Points;
using Sgr.Persistence;

namespace Sgr.Modules.Surveys.Application;

/// <summary>
/// US-16 — Cola de fotos pendientes de georreferenciar manualmente.
/// Lista las fotos sin punto asignado en un survey y permite resolverlas
/// asociándolas a un Punto existente cercano (≤ radio de la plantilla)
/// o creando uno nuevo si no hay candidato.
/// </summary>
public interface IPendingGeoPhotoService
{
    Task<IReadOnlyList<PendingGeoPhotoDto>> ListAsync(
        Guid surveyId, CurrentUserContext actor, CancellationToken ct = default);

    Task<GeoResolveResult> ResolveAsync(
        Guid photoId, decimal latitude, decimal longitude,
        CurrentUserContext actor, CancellationToken ct = default);
}

public sealed record PendingGeoPhotoDto(
    Guid Id,
    Guid SurveyId,
    string AdapterName,
    long SizeBytes,
    string? Comment,
    string? OriginalFileName,
    DateTime CreatedAt);

public sealed record GeoResolveResult(
    Guid PhotoId,
    Guid PointId,
    bool PointWasCreated);

public sealed class PendingGeoPhotoService : IPendingGeoPhotoService
{
    private readonly SgrDbContext _db;
    private readonly IDateTimeProvider _clock;

    public PendingGeoPhotoService(SgrDbContext db, IDateTimeProvider clock)
    {
        _db = db;
        _clock = clock;
    }

    public async Task<IReadOnlyList<PendingGeoPhotoDto>> ListAsync(
        Guid surveyId, CurrentUserContext actor, CancellationToken ct = default)
    {
        // Visibilidad: relevadores ven sus propias subidas; jefes/admin ven todo el survey.
        var query = _db.Photos.AsNoTracking()
            .Where(p => p.SurveyId == surveyId && p.PointId == null && !p.IsDeleted);

        var rows = await query
            .OrderBy(p => p.CreatedAt)
            .Select(p => new
            {
                p.Id, p.SurveyId, p.AdapterName, p.SizeBytes,
                p.Comment, p.MetadataJson, p.CreatedAt,
            })
            .ToListAsync(ct);

        return rows.Select(r => new PendingGeoPhotoDto(
            r.Id, r.SurveyId!.Value, r.AdapterName, r.SizeBytes,
            r.Comment, ExtractOriginalFileName(r.MetadataJson), r.CreatedAt)).ToList();
    }

    public async Task<GeoResolveResult> ResolveAsync(
        Guid photoId, decimal latitude, decimal longitude,
        CurrentUserContext actor, CancellationToken ct = default)
    {
        if (latitude is < -90 or > 90)
            throw new SurveyException(SurveyErrorCode.InvalidPayload, "Latitud fuera de rango.");
        if (longitude is < -180 or > 180)
            throw new SurveyException(SurveyErrorCode.InvalidPayload, "Longitud fuera de rango.");

        var photo = await _db.Photos.FirstOrDefaultAsync(p => p.Id == photoId && !p.IsDeleted, ct)
            ?? throw new SurveyException(SurveyErrorCode.NotFound, $"Photo {photoId} no existe.");

        if (!photo.IsPendingGeo)
            throw new SurveyException(SurveyErrorCode.InvalidPayload,
                "La foto ya está asociada a un punto.");
        if (photo.SurveyId is null)
            throw new SurveyException(SurveyErrorCode.InvalidPayload,
                "La foto no tiene survey de origen.");

        var surveyId = photo.SurveyId.Value;
        var radiusM = await ResolveMobileRadiusAsync(surveyId, ct);
        var now = _clock.UtcNow;

        // Buscar Punto cercano dentro del radio en el mismo survey.
        // Si hay → asocia. Si no → crea nuevo en (lat, lng).
        var existing = await _db.Points.AsNoTracking()
            .Where(p => p.SurveyId == surveyId && !p.IsDeleted)
            .ToListAsync(ct);
        var match = existing.FirstOrDefault(p =>
            HaversineMeters(p.Latitude, p.Longitude, latitude, longitude) <= (double)radiusM);

        Guid pointId;
        bool pointCreated;
        if (match is not null)
        {
            pointId = match.Id;
            pointCreated = false;
        }
        else
        {
            pointId = Guid.NewGuid();
            var point = Point.Create(
                id: pointId, surveyId: surveyId,
                latitude: latitude, longitude: longitude, accuracyM: null,
                createdBy: actor.UserId,
                origin: AuditOrigin.WebManualUpload,
                captureMode: CaptureModes.Web,
                deviceId: null, createdAt: now);
            _db.Points.Add(point);
            _db.AuditEvents.Add(AuditEvent.Create(
                id: Guid.NewGuid(),
                entityType: AuditEntityType.Point,
                entityId: point.Id,
                eventType: AuditEventType.Created,
                fieldKey: null, oldValueJson: null,
                newValueJson: System.Text.Json.JsonSerializer.Serialize(new
                {
                    surveyId, latitude, longitude,
                    captureMode = point.CaptureMode,
                    reason = "geo-resolve",
                }),
                authorId: actor.UserId,
                origin: AuditOrigin.WebManualUpload,
                deviceId: null, timestampOriginal: now, appliedAt: now));
            pointCreated = true;
        }

        // Re-cargamos la foto con tracking para asignar.
        var tracked = await _db.Photos.FirstAsync(p => p.Id == photoId, ct);
        tracked.AssignToPoint(pointId);

        _db.AuditEvents.Add(AuditEvent.Create(
            id: Guid.NewGuid(),
            entityType: AuditEntityType.Photo,
            entityId: tracked.Id,
            eventType: AuditEventType.FieldUpdated,
            fieldKey: "pointId",
            oldValueJson: "null",
            newValueJson: System.Text.Json.JsonSerializer.Serialize(pointId),
            authorId: actor.UserId,
            origin: AuditOrigin.WebManualUpload,
            deviceId: null, timestampOriginal: now, appliedAt: now));

        await _db.SaveChangesAsync(ct);

        return new GeoResolveResult(photoId, pointId, pointCreated);
    }

    private async Task<decimal> ResolveMobileRadiusAsync(Guid surveyId, CancellationToken ct)
    {
        try
        {
            var versionId = await _db.Surveys.AsNoTracking()
                .Where(s => s.Id == surveyId)
                .Select(s => s.TemplateVersionId)
                .FirstOrDefaultAsync(ct);
            if (versionId == Guid.Empty) return 10m;

            var json = await _db.TemplateVersions.AsNoTracking()
                .Where(v => v.Id == versionId)
                .Select(v => v.CaptureParamsJson)
                .FirstOrDefaultAsync(ct);
            if (string.IsNullOrWhiteSpace(json)) return 10m;

            using var doc = System.Text.Json.JsonDocument.Parse(json);
            if (doc.RootElement.TryGetProperty("movil_radius_m", out var prop) &&
                prop.TryGetDecimal(out var r) && r > 0) return r;
        }
        catch { /* fallback */ }
        return 10m;
    }

    private static string? ExtractOriginalFileName(string metadataJson)
    {
        if (string.IsNullOrWhiteSpace(metadataJson)) return null;
        try
        {
            using var doc = System.Text.Json.JsonDocument.Parse(metadataJson);
            if (doc.RootElement.TryGetProperty("originalFileName", out var prop) &&
                prop.ValueKind == System.Text.Json.JsonValueKind.String)
                return prop.GetString();
        }
        catch { /* ignored */ }
        return null;
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
