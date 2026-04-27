namespace Sgr.Modules.Sync.Application;

/// <summary>
/// Evento serializado tal como llega al endpoint <c>POST /api/v1/sync/push</c>.
/// Contrato definido en docs/05_arquitectura_tecnica/contratos-interfaces_v1.0.md.
/// </summary>
public sealed record SyncEventDto(
    Guid EventId,
    string EntityType,        // "survey" | "point" | "photo"
    Guid EntityId,
    string EventType,         // "created" | "field_updated" | "deleted" | "restored" | "merged"
    string? Field,            // e.g. "title" | "description" | "coords"
    string? OldValueJson,
    string? NewValueJson,
    Guid AuthorId,
    string Origin,            // mobile_capture | mobile_edit | web_edit | web_manual_upload
    string? DeviceId,
    DateTime TimestampOriginal);

/// <summary>
/// Resultado por evento del push.
/// </summary>
public sealed record SyncEventResult(
    Guid EventId,
    SyncOutcome Outcome,
    string? Message);

public enum SyncOutcome
{
    Applied = 1,           // Aplicado por primera vez
    Idempotent = 2,        // Ya existía con (event_id, timestamp_original) — no-op
    LosesLWW = 3,          // Llegó pero el valor actual es más reciente — no se aplica
    LosesOwnerPrecedence = 4, // Edición del dueño prevaleció sobre la del colaborador
    RejectedPostClose = 5, // RN-08: el survey está cerrado y este evento es posterior
    RejectedNotFound = 6,  // Entidad referenciada no existe
    RejectedInvalid = 7,   // Validación de payload
    RejectedForbidden = 8, // RN-01 / US-14: el actor no tiene permiso para mutar el punto
}

public sealed record SyncPushResponse(
    int Received,
    int Applied,
    int Skipped,
    IReadOnlyList<SyncEventResult> Results);
