using System.Text.Json;
using Sgr.Frontend.Mobile.Api;
using Sgr.Frontend.Mobile.Auth;

namespace Sgr.Frontend.Mobile.Outbox;

/// <summary>
/// Helper de alto nivel: encola eventos en el outbox listos para que el
/// drainer los push al backend.
/// </summary>
public interface IOutboxEnqueueService
{
    /// <summary>
    /// Crea un evento <c>point.created</c> y lo persiste en el outbox local.
    /// La operación es atómica: o se persiste localmente o falla la llamada.
    /// </summary>
    Task<string> EnqueuePointCreatedAsync(
        Guid surveyId,
        Guid pointId,
        decimal latitude,
        decimal longitude,
        decimal? accuracyM,
        string captureMode,
        DateTime timestamp);

    /// <summary>
    /// E.3.3: encola la subida de una foto capturada. La foto física tiene que estar
    /// ya copiada a una ubicación persistente (no temp dir): el caller pasa la ruta
    /// absoluta vía <paramref name="localFilePath"/>. Después de subirse, el drainer borra el archivo.
    /// </summary>
    Task<string> EnqueuePhotoCapturedAsync(
        Guid surveyId,
        Guid pointId,
        Guid photoId,
        string localFilePath,
        string fileName,
        string mimeType,
        string? comment,
        DateTime capturedAt);
}

public sealed class OutboxEnqueueService : IOutboxEnqueueService
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly IPendingOperationStore _store;
    private readonly IMobileTokenStore _tokens;
    private readonly IDeviceIdProvider _device;

    public OutboxEnqueueService(
        IPendingOperationStore store,
        IMobileTokenStore tokens,
        IDeviceIdProvider device)
    {
        _store = store;
        _tokens = tokens;
        _device = device;
    }

    public async Task<string> EnqueuePointCreatedAsync(
        Guid surveyId,
        Guid pointId,
        decimal latitude,
        decimal longitude,
        decimal? accuracyM,
        string captureMode,
        DateTime timestamp)
    {
        var session = await _tokens.GetAsync()
            ?? throw new InvalidOperationException("Sesión no disponible para encolar eventos.");

        var ev = new SyncEventDto(
            EventId: Guid.NewGuid(),
            EntityType: "point",
            EntityId: pointId,
            EventType: "created",
            Field: null,
            OldValueJson: null,
            NewValueJson: JsonSerializer.Serialize(new
            {
                surveyId,
                latitude,
                longitude,
                accuracyM,
                captureMode,
            }, JsonOptions),
            AuthorId: session.UserId,
            Origin: "mobile_capture",
            DeviceId: _device.GetDeviceId(),
            TimestampOriginal: timestamp);

        var eventsJson = JsonSerializer.Serialize(new[] { ev }, JsonOptions);
        return await _store.EnqueueAsync(surveyId.ToString(), eventsJson, DateTime.UtcNow);
    }

    public async Task<string> EnqueuePhotoCapturedAsync(
        Guid surveyId,
        Guid pointId,
        Guid photoId,
        string localFilePath,
        string fileName,
        string mimeType,
        string? comment,
        DateTime capturedAt)
    {
        if (!File.Exists(localFilePath))
            throw new FileNotFoundException("La foto local no existe.", localFilePath);

        var session = await _tokens.GetAsync()
            ?? throw new InvalidOperationException("Sesión no disponible para encolar fotos.");

        var metadata = new PhotoUploadMetadata(
            PhotoId: photoId,
            PointId: pointId,
            FileName: fileName,
            MimeType: mimeType,
            Comment: comment,
            Origin: "mobile_capture",
            AuthorId: session.UserId,
            DeviceId: _device.GetDeviceId(),
            CapturedAtUtc: capturedAt);

        var metadataJson = JsonSerializer.Serialize(metadata, JsonOptions);
        return await _store.EnqueuePhotoAsync(surveyId.ToString(), metadataJson, localFilePath, DateTime.UtcNow);
    }
}

/// <summary>
/// Persistido en <c>PendingOperation.EventsJson</c> cuando <c>Kind = photo_upload</c>.
/// El drainer lo deserializa para construir el multipart hacia <c>POST /api/v1/photos</c>.
/// </summary>
public sealed record PhotoUploadMetadata(
    Guid PhotoId,
    Guid PointId,
    string FileName,
    string MimeType,
    string? Comment,
    string Origin,
    Guid AuthorId,
    string? DeviceId,
    DateTime CapturedAtUtc);
