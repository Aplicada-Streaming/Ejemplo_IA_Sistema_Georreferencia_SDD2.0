using SQLite;

namespace Sgr.Frontend.Mobile.Outbox;

/// <summary>
/// Outbox local del móvil. Cada fila representa una operación pendiente al backend.
///
/// Hay dos clases de operaciones (campo <see cref="Kind"/>):
///   • <c>sync_event</c> — uno o más <c>SyncEventDto</c> serializados en <see cref="EventsJson"/>,
///     se pushean a <c>POST /api/v1/sync/push</c> (idempotente por EventId, RN-06).
///   • <c>photo_upload</c> — un binario referenciado por <see cref="LocalFilePath"/>
///     más metadata en <see cref="EventsJson"/>, se sube a <c>POST /api/v1/photos</c>
///     (multipart). Idempotente por <c>PhotoId</c>: si el server ya tiene esa foto,
///     responde 201/conflict-OK y el outbox marca como Sent.
///
/// Modelo en SQLite — referencia: PROJECT-BRIEF Sec. 5.2 + RN-06.
/// </summary>
public sealed class PendingOperation
{
    /// <summary>GUID generado en cliente. Idempotency key: si el push se reenvía, el backend lo deduplica.</summary>
    [PrimaryKey]
    public string Id { get; set; } = default!;

    /// <summary>Survey al que pertenece la operación (para filtrar / debug).</summary>
    [Indexed]
    public string SurveyId { get; set; } = default!;

    /// <summary>
    /// Tipo de operación: <see cref="OperationKind.SyncEvent"/> o
    /// <see cref="OperationKind.PhotoUpload"/>. Default <c>sync_event</c> para
    /// retro-compat con filas pre-3.3.
    /// </summary>
    [Indexed]
    public string Kind { get; set; } = OperationKind.SyncEvent;

    /// <summary>
    /// Para <c>sync_event</c>: array JSON de SyncEventDto.
    /// Para <c>photo_upload</c>: objeto JSON con metadata
    /// (photoId, pointId, comment, mimeType, fileName, capturedAt, origin).
    /// </summary>
    public string EventsJson { get; set; } = default!;

    /// <summary>
    /// Sólo para <c>photo_upload</c>: ruta absoluta al archivo local persistente
    /// dentro de <c>FileSystem.AppDataDirectory</c>. NULL para <c>sync_event</c>.
    /// El drainer lee este archivo, lo sube vía multipart y lo borra al confirmarse.
    /// </summary>
    public string? LocalFilePath { get; set; }

    /// <summary>pending | in_flight | sent | error | terminal_error</summary>
    [Indexed]
    public string Status { get; set; } = OutboxStatus.Pending;

    public int Attempts { get; set; }

    public string? LastError { get; set; }

    /// <summary>Cuando volver a intentar (UTC). Sentinel <see cref="DateTime.MinValue"/> para "ya".</summary>
    [Indexed]
    public DateTime NextRetryAt { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? LastAttemptAt { get; set; }

    public DateTime? SentAt { get; set; }
}

public static class OperationKind
{
    public const string SyncEvent = "sync_event";
    public const string PhotoUpload = "photo_upload";
}

public static class OutboxStatus
{
    public const string Pending = "pending";
    public const string InFlight = "in_flight";
    public const string Sent = "sent";
    public const string Error = "error";
    public const string TerminalError = "terminal_error";

    public static readonly IReadOnlySet<string> All =
        new HashSet<string> { Pending, InFlight, Sent, Error, TerminalError };
}
