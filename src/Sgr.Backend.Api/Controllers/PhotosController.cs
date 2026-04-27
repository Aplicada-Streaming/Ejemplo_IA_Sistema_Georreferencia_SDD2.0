using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Sgr.Domain.Audit;
using Sgr.Domain.Common;
using Sgr.Domain.Photos;
using Sgr.Modules.Storage;
using Sgr.Modules.Surveys.Application;
using Sgr.Persistence;

namespace Sgr.Backend.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/v1/photos")]
public sealed class PhotosController : ControllerBase
{
    private readonly SgrDbContext _db;
    private readonly IPhotoStorageAdapterFactory _factory;
    private readonly IDateTimeProvider _clock;
    private readonly IPendingGeoPhotoService _pendingGeo;

    public PhotosController(
        SgrDbContext db,
        IPhotoStorageAdapterFactory factory,
        IDateTimeProvider clock,
        IPendingGeoPhotoService pendingGeo)
    {
        _db = db;
        _factory = factory;
        _clock = clock;
        _pendingGeo = pendingGeo;
    }

    /// <summary>
    /// Sube una foto asociada a un Punto. La binaria se persiste en el adapter activo
    /// (según SystemConfig). El registro de Photo guarda <c>adapter_name</c> + <c>adapter_ref</c>
    /// para que la lectura siga funcionando aunque la configuración cambie luego (RN-12).
    /// </summary>
    [HttpPost]
    [Consumes("multipart/form-data")]
    [ProducesResponseType(typeof(PhotoCreatedDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PhotoCreatedDto>> Upload(
        [FromForm] PhotoUploadForm form,
        CancellationToken ct)
    {
        if (form.PointId == Guid.Empty)
            return BadRequest(Problem(StatusCodes.Status400BadRequest, "Bad Request", "pointId requerido."));
        if (form.File is null || form.File.Length == 0)
            return BadRequest(Problem(StatusCodes.Status400BadRequest, "Bad Request", "Archivo requerido."));

        var point = await _db.Points.AsNoTracking().FirstOrDefaultAsync(p => p.Id == form.PointId, ct);
        if (point is null)
            return NotFound(Problem(StatusCodes.Status404NotFound, "Point not found",
                $"Point {form.PointId} no existe."));

        var current = CurrentUserAccessor.FromPrincipal(User);

        var adapter = _factory.GetActive();
        await using var content = form.File.OpenReadStream();
        var stored = await adapter.StoreAsync(content, new StorageHints(
            SuggestedFolder: $"surveys/{point.SurveyId}/{form.PointId}",
            ContentType: form.File.ContentType ?? "application/octet-stream",
            FileName: form.File.FileName), ct);

        var photoId = form.PhotoId ?? Guid.NewGuid();
        var now = _clock.UtcNow;
        var photo = Photo.Create(
            id: photoId,
            pointId: form.PointId,
            adapterName: stored.AdapterName,
            adapterRef: stored.AdapterRef,
            sizeBytes: stored.SizeBytes,
            contentHash: stored.ContentHash,
            metadataJson: form.MetadataJson ?? "{}",
            createdBy: current.UserId,
            origin: form.Origin ?? AuditOrigin.WebManualUpload,
            createdAt: now,
            comment: form.Comment);

        _db.Photos.Add(photo);

        // Audit append-only.
        var audit = AuditEvent.Create(
            id: Guid.NewGuid(),
            entityType: AuditEntityType.Photo,
            entityId: photo.Id,
            eventType: AuditEventType.Created,
            fieldKey: null,
            oldValueJson: null,
            newValueJson: System.Text.Json.JsonSerializer.Serialize(new
            {
                photo.PointId,
                photo.AdapterName,
                photo.AdapterRef,
                photo.SizeBytes,
                photo.ContentHash,
                photo.Comment,
            }),
            authorId: current.UserId,
            origin: photo.Origin,
            deviceId: null,
            timestampOriginal: now,
            appliedAt: now);
        _db.AuditEvents.Add(audit);

        await _db.SaveChangesAsync(ct);

        return CreatedAtAction(nameof(GetContent), new { id = photo.Id },
            new PhotoCreatedDto(photo.Id, stored.AdapterName, stored.AdapterRef, stored.SizeBytes, stored.ContentHash));
    }

    /// <summary>
    /// Lista las fotos asociadas a un punto, ordenadas por CreatedAt ASC. Devuelve metadata
    /// solamente; el binario se baja con <see cref="GetContent"/> usando el ID de cada item.
    /// </summary>
    [HttpGet("/api/v1/points/{pointId:guid}/photos")]
    [ProducesResponseType(typeof(IReadOnlyList<PhotoSummaryDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<PhotoSummaryDto>>> ListByPoint(
        Guid pointId,
        CancellationToken ct)
    {
        var photos = await _db.Photos.AsNoTracking()
            .Where(p => p.PointId == pointId && !p.IsDeleted)
            .OrderBy(p => p.CreatedAt)
            .Select(p => new PhotoSummaryDto(
                p.Id, p.PointId, p.AdapterName, p.SizeBytes, p.ContentHash,
                p.Comment, p.CreatedBy, p.Origin, p.CreatedAt))
            .ToListAsync(ct);
        return Ok(photos);
    }

    /// <summary>
    /// Devuelve el binario de la foto. Selecciona el adapter por <c>adapter_name</c> registrado
    /// en la entidad Photo, NO el activo del sistema (RN-12). Esto permite leer fotos viejas
    /// aunque el sistema haya migrado a otro storage.
    /// </summary>
    [HttpGet("{id:guid}/content")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetContent(Guid id, CancellationToken ct)
    {
        var photo = await _db.Photos.AsNoTracking().FirstOrDefaultAsync(p => p.Id == id && !p.IsDeleted, ct);
        if (photo is null) return NotFound();

        var adapter = _factory.GetByName(photo.AdapterName);
        var stream = await adapter.ReadAsync(photo.AdapterRef, ct);
        return File(stream, "application/octet-stream", photo.AdapterRef.Split('/').LastOrDefault() ?? "photo.bin");
    }

    /// <summary>
    /// US-16 — Resuelve la georreferencia manual de una foto pendiente. Si las coordenadas
    /// caen dentro del radio del modo recorrido respecto a algún Punto del survey, se asocia
    /// a ese Punto; si no, se crea uno nuevo.
    /// </summary>
    [HttpPost("{id:guid}/geo-resolve")]
    [ProducesResponseType(typeof(GeoResolveResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<GeoResolveResult>> GeoResolve(
        Guid id,
        [FromBody] GeoResolveDto body,
        CancellationToken ct)
    {
        var current = CurrentUserAccessor.FromPrincipal(User);
        var actor = new CurrentUserContext(current.UserId, current.Role, current.AreaId);
        var result = await _pendingGeo.ResolveAsync(id, body.Latitude, body.Longitude, actor, ct);
        return Ok(result);
    }

    private static ProblemDetails Problem(int status, string title, string detail) => new()
    {
        Status = status,
        Title = title,
        Detail = detail,
    };
}

public sealed class GeoResolveDto
{
    [Range(-90, 90)]
    public decimal Latitude { get; set; }

    [Range(-180, 180)]
    public decimal Longitude { get; set; }
}

public sealed class PhotoUploadForm
{
    public IFormFile? File { get; set; }
    public Guid PointId { get; set; }
    public Guid? PhotoId { get; set; }
    public string? Comment { get; set; }
    public string? MetadataJson { get; set; }
    public string? Origin { get; set; }
}

public sealed record PhotoCreatedDto(
    Guid Id,
    string AdapterName,
    string AdapterRef,
    long SizeBytes,
    string ContentHash);

/// <summary>
/// Resumen de foto para listados. NO incluye <c>AdapterRef</c> (es interno de storage)
/// ni <c>MetadataJson</c> (puede ser grande). El cliente baja el binario con
/// <c>GET /api/v1/photos/{id}/content</c>.
/// </summary>
public sealed record PhotoSummaryDto(
    Guid Id,
    Guid? PointId,
    string AdapterName,
    long SizeBytes,
    string ContentHash,
    string? Comment,
    Guid CreatedBy,
    string Origin,
    DateTime CreatedAt);
