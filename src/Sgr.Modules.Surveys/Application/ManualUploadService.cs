using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Sgr.Domain.Audit;
using Sgr.Domain.Common;
using Sgr.Domain.Identity;
using Sgr.Domain.Photos;
using Sgr.Domain.Points;
using Sgr.Domain.Surveys;
using Sgr.Modules.Storage;
using Sgr.Modules.Storage.Imaging;
using Sgr.Persistence;

namespace Sgr.Modules.Surveys.Application;

/// <summary>
/// US-15 — Subida en lote desde web con extracción EXIF.
///
/// Por cada foto del lote:
/// 1. Calcula hash + lo persiste en el storage activo (RN-12).
/// 2. Lee EXIF (lat/lng/timestamp) — si falla, queda en cola pendiente (US-16).
/// 3. Agrupa según <see cref="ManualUploadMode"/>:
///    - <c>Detenido</c>: todas las fotos (con o sin EXIF) van al mismo Punto. Si las fotos
///      tienen EXIF, el Punto se crea en el centroide; si no, en (0,0) — el usuario puede
///      reubicarlo después.
///    - <c>Recorrido</c>: cluster por proximidad espacial (radio configurado en plantilla)
///      + temporal (ventana <c>movil_radius_m</c> + 5 min). Las fotos sin EXIF van a la cola.
///    - <c>UnaPorFoto</c>: cada foto con EXIF crea su propio Punto; las sin EXIF a la cola.
/// 4. Genera AuditEvent <c>created</c> por cada Punto y Foto (RN-10).
/// </summary>
public interface IManualUploadService
{
    Task<ManualUploadResult> UploadAsync(
        ManualUploadRequest request,
        CurrentUserContext actor,
        CancellationToken ct = default);
}

public enum ManualUploadMode
{
    Detenido = 1,
    Recorrido = 2,
    UnaPorFoto = 3,
}

public sealed record ManualUploadFile(
    string FileName,
    string ContentType,
    Stream Content);

public sealed record ManualUploadRequest(
    Guid SurveyId,
    ManualUploadMode Mode,
    IReadOnlyList<ManualUploadFile> Files);

public sealed record ManualUploadResult(
    int FilesReceived,
    int PhotosWithGps,
    int PhotosPendingGeo,
    int PointsCreated);

public sealed class ManualUploadService : IManualUploadService
{
    private readonly SgrDbContext _db;
    private readonly IPhotoStorageAdapterFactory _storageFactory;
    private readonly IExifReader _exif;
    private readonly IDateTimeProvider _clock;
    private readonly ILogger<ManualUploadService> _logger;

    public ManualUploadService(
        SgrDbContext db,
        IPhotoStorageAdapterFactory storageFactory,
        IExifReader exif,
        IDateTimeProvider clock,
        ILogger<ManualUploadService> logger)
    {
        _db = db;
        _storageFactory = storageFactory;
        _exif = exif;
        _clock = clock;
        _logger = logger;
    }

    public async Task<ManualUploadResult> UploadAsync(
        ManualUploadRequest request,
        CurrentUserContext actor,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(actor);

        if (request.Files.Count == 0)
            throw new SurveyException(SurveyErrorCode.InvalidPayload,
                "El lote no contiene archivos.");

        var survey = await _db.Surveys.FirstOrDefaultAsync(s => s.Id == request.SurveyId && !s.IsDeleted, ct)
            ?? throw new SurveyException(SurveyErrorCode.NotFound,
                $"Survey {request.SurveyId} no existe.");

        // Permisos: admin / jefe del área / dueño / colaborador asignado pueden subir.
        // Para Slice 7 simplificamos: cualquiera con visibilidad sobre el survey puede.
        // Visibilidad ya validada por roles; el filtrado fino lo hace IListSurveysService.
        // Reusamos la regla "no cerrado" (RN-08) explícita.
        if (survey.Status == SurveyStatus.Cerrado)
            throw new SurveyException(SurveyErrorCode.Forbidden,
                "No se puede subir fotos a un relevamiento cerrado.");

        var radiusM = await ResolveMobileRadiusAsync(survey.TemplateVersionId, ct);
        var adapter = _storageFactory.GetActive();
        var now = _clock.UtcNow;

        // 1. Subir TODAS las fotos al storage + leer EXIF — bookkeeping en memoria.
        var processed = new List<ProcessedFile>(request.Files.Count);
        foreach (var f in request.Files)
        {
            // Necesitamos el stream dos veces: una para EXIF (sin consumir), otra para storage.
            // Buffereamos en memoria — los lotes manuales son chicos comparados con video.
            await using var bufferedStream = new MemoryStream();
            await f.Content.CopyToAsync(bufferedStream, ct);
            var bytes = bufferedStream.ToArray();

            ExifData exif;
            try
            {
                using var exifMs = new MemoryStream(bytes);
                exif = _exif.Read(exifMs);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "EXIF read raised on '{File}', falling back to none.", f.FileName);
                exif = ExifData.None;
            }

            using var storeMs = new MemoryStream(bytes);
            var stored = await adapter.StoreAsync(storeMs, new StorageHints(
                SuggestedFolder: $"surveys/{survey.Id}/manual",
                ContentType: f.ContentType,
                FileName: f.FileName), ct);

            processed.Add(new ProcessedFile(f.FileName, exif, stored, bytes.LongLength));
        }

        // 2. Agrupar / crear puntos según modo.
        var (pointsCreated, withGps, pending) = request.Mode switch
        {
            ManualUploadMode.Detenido => PersistDetenido(survey, processed, actor, now),
            ManualUploadMode.Recorrido => PersistRecorrido(survey, processed, actor, now, radiusM),
            ManualUploadMode.UnaPorFoto => PersistUnaPorFoto(survey, processed, actor, now),
            _ => throw new SurveyException(SurveyErrorCode.InvalidPayload,
                $"Modo de carga inválido: {request.Mode}"),
        };

        await _db.SaveChangesAsync(ct);

        return new ManualUploadResult(
            FilesReceived: request.Files.Count,
            PhotosWithGps: withGps,
            PhotosPendingGeo: pending,
            PointsCreated: pointsCreated);
    }

    private (int pointsCreated, int withGps, int pending) PersistDetenido(
        Survey survey,
        List<ProcessedFile> files,
        CurrentUserContext actor,
        DateTime now)
    {
        var withGps = files.Count(f => f.Exif.HasGps);
        var pending = files.Count - withGps;

        // Centroide de las fotos con GPS — si ninguna lo tiene, todas van a la cola.
        if (withGps == 0)
        {
            foreach (var f in files) AddPendingPhoto(survey, f, actor, now);
            return (0, withGps, pending);
        }

        var avgLat = files.Where(f => f.Exif.HasGps).Average(f => f.Exif.Latitude!.Value);
        var avgLng = files.Where(f => f.Exif.HasGps).Average(f => f.Exif.Longitude!.Value);

        var pointId = Guid.NewGuid();
        var earliest = files.Min(f => f.Exif.TakenAtUtc) ?? now;
        var point = Point.Create(
            id: pointId, surveyId: survey.Id,
            latitude: avgLat, longitude: avgLng, accuracyM: null,
            createdBy: actor.UserId,
            origin: AuditOrigin.WebManualUpload,
            captureMode: CaptureModes.Web,
            deviceId: null,
            createdAt: earliest);
        _db.Points.Add(point);
        AddPointAudit(point, actor, earliest);

        foreach (var f in files)
        {
            if (f.Exif.HasGps)
                AddAssignedPhoto(point.Id, survey, f, actor, now);
            else
                AddPendingPhoto(survey, f, actor, now);
        }
        return (1, withGps, pending);
    }

    private (int pointsCreated, int withGps, int pending) PersistRecorrido(
        Survey survey,
        List<ProcessedFile> files,
        CurrentUserContext actor,
        DateTime now,
        decimal radiusM)
    {
        var withGps = 0;
        var pending = 0;

        // Las sin EXIF van a la cola.
        foreach (var f in files.Where(f => !f.Exif.HasGps))
        {
            AddPendingPhoto(survey, f, actor, now);
            pending++;
        }

        // Cluster simple sobre las que tienen GPS, ordenadas por timestamp.
        var withExif = files
            .Where(f => f.Exif.HasGps)
            .OrderBy(f => f.Exif.TakenAtUtc ?? now)
            .ToList();

        var clusters = new List<ClusterPoint>();
        const int timeWindowMinutes = 5;

        foreach (var f in withExif)
        {
            var lat = f.Exif.Latitude!.Value;
            var lng = f.Exif.Longitude!.Value;
            var ts = f.Exif.TakenAtUtc ?? now;

            var match = clusters.FirstOrDefault(c =>
                HaversineMeters(c.Latitude, c.Longitude, lat, lng) <= (double)radiusM &&
                Math.Abs((ts - c.LastTimestamp).TotalMinutes) <= timeWindowMinutes);

            if (match is null)
            {
                var pointId = Guid.NewGuid();
                var point = Point.Create(
                    id: pointId, surveyId: survey.Id,
                    latitude: lat, longitude: lng, accuracyM: null,
                    createdBy: actor.UserId,
                    origin: AuditOrigin.WebManualUpload,
                    captureMode: CaptureModes.Movil,
                    deviceId: null,
                    createdAt: ts);
                _db.Points.Add(point);
                AddPointAudit(point, actor, ts);
                clusters.Add(new ClusterPoint(pointId, lat, lng, ts));
                AddAssignedPhoto(pointId, survey, f, actor, now);
            }
            else
            {
                AddAssignedPhoto(match.PointId, survey, f, actor, now);
                match.LastTimestamp = ts;
            }
            withGps++;
        }

        return (clusters.Count, withGps, pending);
    }

    private (int pointsCreated, int withGps, int pending) PersistUnaPorFoto(
        Survey survey,
        List<ProcessedFile> files,
        CurrentUserContext actor,
        DateTime now)
    {
        var withGps = 0;
        var pending = 0;
        var pointsCreated = 0;
        foreach (var f in files)
        {
            if (!f.Exif.HasGps)
            {
                AddPendingPhoto(survey, f, actor, now);
                pending++;
                continue;
            }
            var ts = f.Exif.TakenAtUtc ?? now;
            var pointId = Guid.NewGuid();
            var point = Point.Create(
                id: pointId, surveyId: survey.Id,
                latitude: f.Exif.Latitude!.Value, longitude: f.Exif.Longitude!.Value,
                accuracyM: null,
                createdBy: actor.UserId,
                origin: AuditOrigin.WebManualUpload,
                captureMode: CaptureModes.Web,
                deviceId: null, createdAt: ts);
            _db.Points.Add(point);
            AddPointAudit(point, actor, ts);
            AddAssignedPhoto(pointId, survey, f, actor, now);
            pointsCreated++;
            withGps++;
        }
        return (pointsCreated, withGps, pending);
    }

    private void AddPendingPhoto(Survey survey, ProcessedFile f, CurrentUserContext actor, DateTime now)
    {
        var photoId = Guid.NewGuid();
        var photo = Photo.CreatePendingGeo(
            id: photoId, surveyId: survey.Id,
            adapterName: f.Stored.AdapterName, adapterRef: f.Stored.AdapterRef,
            sizeBytes: f.Stored.SizeBytes, contentHash: f.Stored.ContentHash,
            metadataJson: BuildMetadataJson(f),
            createdBy: actor.UserId,
            origin: AuditOrigin.WebManualUpload,
            createdAt: now,
            comment: $"Cargada {now:yyyy-MM-dd} desde {f.FileName}");
        _db.Photos.Add(photo);
        AddPhotoAudit(photo, actor, now);
    }

    private void AddAssignedPhoto(Guid pointId, Survey survey, ProcessedFile f, CurrentUserContext actor, DateTime now)
    {
        var photoId = Guid.NewGuid();
        var photo = Photo.Create(
            id: photoId, pointId: pointId,
            adapterName: f.Stored.AdapterName, adapterRef: f.Stored.AdapterRef,
            sizeBytes: f.Stored.SizeBytes, contentHash: f.Stored.ContentHash,
            metadataJson: BuildMetadataJson(f),
            createdBy: actor.UserId,
            origin: AuditOrigin.WebManualUpload,
            createdAt: now,
            comment: $"Cargada {now:yyyy-MM-dd} desde {f.FileName}");
        _db.Photos.Add(photo);
        AddPhotoAudit(photo, actor, now);
    }

    private void AddPointAudit(Point point, CurrentUserContext actor, DateTime ts)
    {
        var audit = AuditEvent.Create(
            id: Guid.NewGuid(),
            entityType: AuditEntityType.Point,
            entityId: point.Id,
            eventType: AuditEventType.Created,
            fieldKey: null, oldValueJson: null,
            newValueJson: System.Text.Json.JsonSerializer.Serialize(new
            {
                surveyId = point.SurveyId,
                latitude = point.Latitude,
                longitude = point.Longitude,
                captureMode = point.CaptureMode,
            }),
            authorId: actor.UserId,
            origin: AuditOrigin.WebManualUpload,
            deviceId: null,
            timestampOriginal: ts,
            appliedAt: _clock.UtcNow);
        _db.AuditEvents.Add(audit);
    }

    private void AddPhotoAudit(Photo photo, CurrentUserContext actor, DateTime now)
    {
        var audit = AuditEvent.Create(
            id: Guid.NewGuid(),
            entityType: AuditEntityType.Photo,
            entityId: photo.Id,
            eventType: AuditEventType.Created,
            fieldKey: null, oldValueJson: null,
            newValueJson: System.Text.Json.JsonSerializer.Serialize(new
            {
                photo.PointId,
                photo.SurveyId,
                photo.AdapterName,
                photo.SizeBytes,
                photo.ContentHash,
            }),
            authorId: actor.UserId,
            origin: AuditOrigin.WebManualUpload,
            deviceId: null,
            timestampOriginal: now,
            appliedAt: now);
        _db.AuditEvents.Add(audit);
    }

    private static string BuildMetadataJson(ProcessedFile f) =>
        System.Text.Json.JsonSerializer.Serialize(new
        {
            originalFileName = f.FileName,
            exif_lat = f.Exif.Latitude,
            exif_lng = f.Exif.Longitude,
            exif_taken_at_utc = f.Exif.TakenAtUtc,
        });

    private async Task<decimal> ResolveMobileRadiusAsync(Guid templateVersionId, CancellationToken ct)
    {
        // Si no podemos leer la plantilla por algún motivo, fallback al default operativo (10m).
        try
        {
            var version = await _db.TemplateVersions.AsNoTracking()
                .FirstOrDefaultAsync(v => v.Id == templateVersionId, ct);
            if (version is null) return 10m;

            using var doc = System.Text.Json.JsonDocument.Parse(version.CaptureParamsJson);
            if (doc.RootElement.TryGetProperty("movil_radius_m", out var prop) &&
                prop.TryGetDecimal(out var r) && r > 0)
                return r;
        }
        catch { /* fallback */ }
        return 10m;
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

    private sealed record ProcessedFile(
        string FileName,
        ExifData Exif,
        StoredPhotoRef Stored,
        long Bytes);

    private sealed class ClusterPoint
    {
        public Guid PointId { get; }
        public decimal Latitude { get; }
        public decimal Longitude { get; }
        public DateTime LastTimestamp { get; set; }

        public ClusterPoint(Guid pointId, decimal lat, decimal lng, DateTime ts)
        {
            PointId = pointId;
            Latitude = lat;
            Longitude = lng;
            LastTimestamp = ts;
        }
    }
}
