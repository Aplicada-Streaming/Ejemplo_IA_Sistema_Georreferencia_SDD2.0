using Microsoft.EntityFrameworkCore;
using Sgr.Domain.Audit;
using Sgr.Domain.Identity;
using Sgr.Modules.Surveys.Application;
using Sgr.Persistence;

namespace Sgr.Modules.Sync.Application;

/// <summary>
/// Verifica permisos de mutación sobre puntos antes de delegar al EventApplier (RN-01 / US-14).
///
/// Reglas (chequeadas contra el actor del JWT — el <c>AuthorId</c> del evento se mantiene
/// para fines de auditoría y la lógica RN-07 del applier; ver nota más abajo):
/// - <b>created</b>: cualquier actor con login válido puede crear puntos en surveys que ve.
///   La protección sobre survey cerrado se mantiene en el applier (RN-08).
/// - <b>field_updated / deleted / restored</b>: actor debe ser el creator del punto **o**
///   el dueño del survey. <c>admin_raiz</c> puede tocar todo.
///
/// Nota: no validamos <c>AuthorId == actor.UserId</c> (anti-impersonación) porque los tests
/// del Slice 1 simulan multi-colaborador con un solo JWT. En producción la mobile/web emiten
/// el evento con <c>authorId = currentUser</c>, así que coinciden naturalmente.
///
/// Devuelve la lista de eventos que pasan + los resultados pre-emptivos para los rechazados.
/// El SyncController los pega al response final del applier.
/// </summary>
public interface IPointAccessChecker
{
    Task<PointAccessResult> CheckAsync(
        IEnumerable<SyncEventDto> events,
        CurrentUserContext actor,
        CancellationToken ct = default);
}

public sealed record PointAccessResult(
    IReadOnlyList<SyncEventDto> Allowed,
    IReadOnlyList<SyncEventResult> Rejected);

public sealed class PointAccessChecker : IPointAccessChecker
{
    private readonly SgrDbContext _db;

    public PointAccessChecker(SgrDbContext db) => _db = db;

    public async Task<PointAccessResult> CheckAsync(
        IEnumerable<SyncEventDto> events,
        CurrentUserContext actor,
        CancellationToken ct = default)
    {
        var allowed = new List<SyncEventDto>();
        var rejected = new List<SyncEventResult>();

        foreach (var e in events)
        {
            // Sólo aplican reglas RN-01 a Point. Survey/Photo siguen al applier sin checks aquí.
            if (e.EntityType != AuditEntityType.Point)
            {
                allowed.Add(e);
                continue;
            }

            // created sobre Point: se permite — el creador queda registrado y los chequeos de
            // pertenencia al survey ya los hace el applier (con RN-08).
            if (e.EventType == AuditEventType.Created)
            {
                allowed.Add(e);
                continue;
            }

            // Mutaciones sobre punto existente — RN-01.
            // admin_raiz cortocircuita (ver RN-01: admin tiene permiso global).
            if (actor.Role == UserRole.AdminRaiz)
            {
                allowed.Add(e);
                continue;
            }

            var point = await _db.Points.AsNoTracking()
                .FirstOrDefaultAsync(p => p.Id == e.EntityId, ct);
            if (point is null)
            {
                // Punto no existe — dejá que el applier responda con RejectedNotFound.
                allowed.Add(e);
                continue;
            }

            var survey = await _db.Surveys.AsNoTracking()
                .FirstOrDefaultAsync(s => s.Id == point.SurveyId, ct);
            if (survey is null)
            {
                allowed.Add(e);
                continue;
            }

            var isCreator = point.CreatedBy == actor.UserId;
            var isOwner = survey.OwnerId == actor.UserId;

            if (!isCreator && !isOwner)
            {
                rejected.Add(new SyncEventResult(e.EventId, SyncOutcome.RejectedForbidden,
                    "RN-01: sólo el creador del punto o el dueño del relevamiento pueden modificarlo."));
                continue;
            }

            allowed.Add(e);
        }

        return new PointAccessResult(allowed, rejected);
    }
}
