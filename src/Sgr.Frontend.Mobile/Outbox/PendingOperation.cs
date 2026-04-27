using SQLite;

namespace Sgr.Frontend.Mobile.Outbox;

/// <summary>
/// Outbox local del móvil. Cada fila representa un push pendiente al backend
/// (uno o más SyncEventDto serializados como JSON en EventsJson).
///
/// Modelo de la tabla local en SQLite — referencia: PROJECT-BRIEF Sec. 5.2 + RN-06.
/// </summary>
public sealed class PendingOperation
{
    /// <summary>GUID generado en cliente. Idempotency key: si el push se reenvía, el backend lo deduplica.</summary>
    [PrimaryKey]
    public string Id { get; set; } = default!;

    /// <summary>Survey al que pertenece la operación (para filtrar / debug).</summary>
    [Indexed]
    public string SurveyId { get; set; } = default!;

    /// <summary>JSON serializado del array de eventos a pushear al backend.</summary>
    public string EventsJson { get; set; } = default!;

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
