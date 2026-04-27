using Microsoft.EntityFrameworkCore;
using Sgr.Domain.Audit;
using Sgr.Domain.Common;
using Sgr.Domain.Conflicts;
using Sgr.Domain.Identity;
using Sgr.Domain.Surveys;
using Sgr.Modules.Surveys.Application;
using Sgr.Persistence;

namespace Sgr.Modules.Sync.Application;

/// <summary>
/// Operaciones sobre Conflictos de sincronización (US-19 / US-20).
///
/// <c>ListAsync</c> filtra por survey + tipo + status (default: pendientes).
///
/// <c>ResolveAsync</c> aplica una de tres acciones:
/// - <c>keep_current</c>: marca <c>resuelto_sin_cambio</c>. No se modifica el modelo.
/// - <c>revert</c> (lww/owner_precedence): genera un nuevo <c>field_updated</c>
///   con el <c>attempted_value</c> del conflicto, autorizado por el actor JWT.
///   Vuelve a pasar por el applier — el valor revertido gana porque su timestamp
///   es ahora el más reciente (US-20 CA-20.2).
/// - <c>revert</c> (post_close): el applier ya descartó la captura. Para realmente
///   aplicarla habría que reabrir el survey — fuera del MVP de Slice 9; por ahora
///   la acción <c>revert</c> sobre un post_close devuelve <c>NotImplemented</c>.
/// </summary>
public interface IConflictsService
{
    Task<IReadOnlyList<ConflictDto>> ListAsync(
        Guid? surveyId,
        string? type,
        string? status,
        CurrentUserContext actor,
        CancellationToken ct = default);

    Task<ConflictDto> ResolveAsync(
        Guid conflictId,
        string action,
        CurrentUserContext actor,
        CancellationToken ct = default);
}

public sealed record ConflictDto(
    Guid Id,
    Guid SurveyId,
    string Type,
    Guid EventId,
    Guid? PointId,
    string? FieldKey,
    Guid AuthorId,
    string? AttemptedValueJson,
    string? CurrentValueJson,
    DateTime AttemptedAtUtc,
    string Status,
    DateTime? ResolvedAtUtc,
    Guid? ResolvedBy,
    string? ResolutionNote);

public enum ConflictActions
{
    KeepCurrent,
    Revert,
}

public sealed class ConflictsService : IConflictsService
{
    private readonly SgrDbContext _db;
    private readonly IEventApplier _applier;
    private readonly IDateTimeProvider _clock;

    public ConflictsService(SgrDbContext db, IEventApplier applier, IDateTimeProvider clock)
    {
        _db = db;
        _applier = applier;
        _clock = clock;
    }

    public async Task<IReadOnlyList<ConflictDto>> ListAsync(
        Guid? surveyId,
        string? type,
        string? status,
        CurrentUserContext actor,
        CancellationToken ct = default)
    {
        var query = _db.Conflicts.AsNoTracking().AsQueryable();
        if (surveyId is not null) query = query.Where(c => c.SurveyId == surveyId);
        if (!string.IsNullOrWhiteSpace(type)) query = query.Where(c => c.Type == type);
        // Default: pendientes. Si el caller manda explícitamente "all" devuelve todo.
        var effectiveStatus = string.IsNullOrWhiteSpace(status) ? ConflictStatus.Pendiente : status;
        if (effectiveStatus != "all")
            query = query.Where(c => c.Status == effectiveStatus);

        // Visibilidad: relevadores ven sólo conflictos de surveys donde participan.
        // Para Slice 9 simplificamos: sólo admin/jefe pueden listar — el endpoint impone Roles.

        var rows = await query
            .OrderByDescending(c => c.CreatedAt)
            .ToListAsync(ct);
        return rows.Select(ToDto).ToList();
    }

    public async Task<ConflictDto> ResolveAsync(
        Guid conflictId,
        string action,
        CurrentUserContext actor,
        CancellationToken ct = default)
    {
        var conflict = await _db.Conflicts.FirstOrDefaultAsync(c => c.Id == conflictId, ct)
            ?? throw new InvalidOperationException($"Conflict {conflictId} no existe.");
        if (conflict.Status != ConflictStatus.Pendiente)
            throw new InvalidOperationException("El conflicto ya estaba resuelto.");

        if (!Enum.TryParse<ConflictActions>(action, ignoreCase: true, out var act))
            throw new ArgumentException($"Acción inválida '{action}'. Usar 'KeepCurrent' | 'Revert'.");

        var now = _clock.UtcNow;
        switch (act)
        {
            case ConflictActions.KeepCurrent:
                conflict.MarkResolved(ConflictStatus.ResueltoSinCambio, actor.UserId, now,
                    note: "Usuario decidió mantener el valor actual.");
                break;

            case ConflictActions.Revert:
                if (conflict.Type == ConflictTypes.PostClose)
                    throw new NotSupportedException(
                        "Para conflictos post-cierre, primero reabrir el survey y reenviar el evento. " +
                        "(Reapertura automática queda fuera del MVP de Slice 9.)");

                if (string.IsNullOrEmpty(conflict.FieldKey) || conflict.PointId is null)
                    throw new InvalidOperationException(
                        "Conflict sin field/point — no se puede revertir.");

                // Generar un nuevo evento field_updated con el attempted_value del conflicto.
                // El actor es el usuario que está resolviendo (no el original) — pasa por el
                // applier y, al ser timestamp posterior, gana por LWW (CA-20.2).
                var revertEvent = new SyncEventDto(
                    EventId: Guid.NewGuid(),
                    EntityType: AuditEntityType.Point,
                    EntityId: conflict.PointId.Value,
                    EventType: AuditEventType.FieldUpdated,
                    Field: conflict.FieldKey,
                    OldValueJson: conflict.CurrentValueJson,
                    NewValueJson: conflict.AttemptedValueJson,
                    AuthorId: actor.UserId,
                    Origin: AuditOrigin.WebEdit,
                    DeviceId: null,
                    TimestampOriginal: now);

                // El applier maneja LWW; si el actor es owner cortocircuita owner_precedence.
                await _applier.ApplyAsync(new[] { revertEvent }, ct);
                conflict.MarkResolved(ConflictStatus.ResueltoRevertido, actor.UserId, now,
                    note: $"Revertido: nuevo evento {revertEvent.EventId}.");
                break;
        }

        await _db.SaveChangesAsync(ct);
        return ToDto(conflict);
    }

    private static ConflictDto ToDto(Conflict c) => new(
        c.Id, c.SurveyId, c.Type, c.EventId, c.PointId, c.FieldKey, c.AuthorId,
        c.AttemptedValueJson, c.CurrentValueJson, c.AttemptedAtUtc, c.Status,
        c.ResolvedAtUtc, c.ResolvedBy, c.ResolutionNote);
}
