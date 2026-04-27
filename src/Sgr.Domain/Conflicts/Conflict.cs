using Sgr.Domain.Common;

namespace Sgr.Domain.Conflicts;

/// <summary>
/// Conflicto de sincronización detectado durante el push (US-19 / RN-07 / RN-08).
/// El applier crea un Conflict cuando el outcome del evento NO fue Applied/Idempotent
/// y la causa no es validación de payload — es decir, el evento llegó OK pero perdió
/// contra otro o fue rechazado por reglas de negocio que ameritan revisión humana.
///
/// El usuario lo resuelve eligiendo:
/// - <c>revert</c>: genera un nuevo <c>field_updated</c> con el valor "perdido" (US-20).
/// - <c>keep_current</c>: deja el valor actual y marca el conflicto como revisado.
/// Para conflictos post-cierre, <c>revert</c> reabre el survey antes de aplicar.
/// </summary>
public sealed class Conflict : Entity
{
    public Guid SurveyId { get; private set; }

    /// <summary>"lww" | "owner_precedence" | "post_close"</summary>
    public string Type { get; private set; } = default!;

    /// <summary>EventId del evento que perdió/fue rechazado (referencia AuditEvent).</summary>
    public Guid EventId { get; private set; }

    /// <summary>Point afectado. Null sólo si el conflicto es a nivel survey.</summary>
    public Guid? PointId { get; private set; }

    /// <summary>Field key (ej. "title", "coords", "condicion_general") cuando aplica.</summary>
    public string? FieldKey { get; private set; }

    public Guid AuthorId { get; private set; }

    /// <summary>Valor que el evento perdedor intentó escribir (JSON).</summary>
    public string? AttemptedValueJson { get; private set; }

    /// <summary>Valor que prevaleció en el modelo central al momento del conflicto (JSON).</summary>
    public string? CurrentValueJson { get; private set; }

    public DateTime AttemptedAtUtc { get; private set; }

    /// <summary>"pendiente" | "resuelto_revertido" | "resuelto_sin_cambio"</summary>
    public string Status { get; private set; } = default!;

    public DateTime? ResolvedAtUtc { get; private set; }
    public Guid? ResolvedBy { get; private set; }
    public string? ResolutionNote { get; private set; }

    private Conflict() { }

    public static Conflict Create(
        Guid id,
        Guid surveyId,
        string type,
        Guid eventId,
        Guid? pointId,
        string? fieldKey,
        Guid authorId,
        string? attemptedValueJson,
        string? currentValueJson,
        DateTime attemptedAtUtc)
    {
        if (id == Guid.Empty) throw new ArgumentException("Id required.", nameof(id));
        if (surveyId == Guid.Empty) throw new ArgumentException("SurveyId required.", nameof(surveyId));
        if (eventId == Guid.Empty) throw new ArgumentException("EventId required.", nameof(eventId));
        if (authorId == Guid.Empty) throw new ArgumentException("AuthorId required.", nameof(authorId));
        if (!ConflictTypes.IsValid(type))
            throw new ArgumentException($"Invalid type '{type}'.", nameof(type));

        return new Conflict
        {
            Id = id,
            SurveyId = surveyId,
            Type = type,
            EventId = eventId,
            PointId = pointId,
            FieldKey = fieldKey,
            AuthorId = authorId,
            AttemptedValueJson = attemptedValueJson,
            CurrentValueJson = currentValueJson,
            AttemptedAtUtc = attemptedAtUtc,
            Status = ConflictStatus.Pendiente,
            CreatedAt = attemptedAtUtc,
        };
    }

    public void MarkResolved(string newStatus, Guid resolvedBy, DateTime nowUtc, string? note = null)
    {
        if (Status != ConflictStatus.Pendiente)
            throw new InvalidOperationException("Conflict already resolved.");
        if (newStatus != ConflictStatus.ResueltoRevertido && newStatus != ConflictStatus.ResueltoSinCambio)
            throw new ArgumentException($"Invalid resolution status '{newStatus}'.", nameof(newStatus));
        Status = newStatus;
        ResolvedBy = resolvedBy;
        ResolvedAtUtc = nowUtc;
        ResolutionNote = note;
    }
}

public static class ConflictTypes
{
    public const string Lww = "lww";
    public const string OwnerPrecedence = "owner_precedence";
    public const string PostClose = "post_close";

    public static readonly IReadOnlySet<string> All = new HashSet<string> { Lww, OwnerPrecedence, PostClose };
    public static bool IsValid(string s) => All.Contains(s);
}

public static class ConflictStatus
{
    public const string Pendiente = "pendiente";
    public const string ResueltoRevertido = "resuelto_revertido";
    public const string ResueltoSinCambio = "resuelto_sin_cambio";
}
