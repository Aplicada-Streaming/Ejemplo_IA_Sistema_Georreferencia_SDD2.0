using Microsoft.EntityFrameworkCore;
using Sgr.Domain.Audit;
using Sgr.Domain.Common;
using Sgr.Domain.Conflicts;
using Sgr.Domain.Identity;
using Sgr.Domain.Points;
using Sgr.Domain.Surveys;
using Sgr.Persistence;

namespace Sgr.Modules.Sync.Application;

public interface IEventApplier
{
    /// <summary>
    /// Aplica una lista de eventos al modelo central. Cada evento:
    /// 1. Si ya existe en AuditEvents (idempotency por EventId) → no-op (Idempotent).
    /// 2. Resuelve LWW por campo: solo aplica si TimestampOriginal es ≥ que el último cambio sobre ese campo.
    /// 3. Precedencia del dueño: en edición del mismo campo, gana el dueño aunque su timestamp sea anterior.
    /// 4. Capturas post-cierre del survey → rechazadas (RN-08).
    /// La transacción es atómica por batch.
    /// </summary>
    Task<SyncPushResponse> ApplyAsync(IEnumerable<SyncEventDto> events, CancellationToken ct = default);
}

public sealed class EventApplier : IEventApplier
{
    private readonly SgrDbContext _db;
    private readonly IDateTimeProvider _clock;

    public EventApplier(SgrDbContext db, IDateTimeProvider clock)
    {
        _db = db;
        _clock = clock;
    }

    public async Task<SyncPushResponse> ApplyAsync(IEnumerable<SyncEventDto> events, CancellationToken ct = default)
    {
        var batch = events.ToList();
        var results = new List<SyncEventResult>(batch.Count);
        int applied = 0, skipped = 0;

        // Aplicar en orden cronológico por timestamp_original (estable contra reenvíos).
        foreach (var e in batch.OrderBy(x => x.TimestampOriginal).ThenBy(x => x.EventId))
        {
            var result = await ApplyOneAsync(e, ct);
            results.Add(result);
            if (result.Outcome == SyncOutcome.Applied) applied++; else skipped++;
        }

        await _db.SaveChangesAsync(ct);

        return new SyncPushResponse(
            Received: batch.Count,
            Applied: applied,
            Skipped: skipped,
            Results: results);
    }

    private async Task<SyncEventResult> ApplyOneAsync(SyncEventDto e, CancellationToken ct)
    {
        if (!Validate(e, out var validationError))
            return new SyncEventResult(e.EventId, SyncOutcome.RejectedInvalid, validationError);

        // RN-06: idempotency por EventId. Si el evento ya existe, no-op.
        var alreadyApplied = await _db.AuditEvents.AsNoTracking().AnyAsync(a => a.Id == e.EventId, ct);
        if (alreadyApplied)
            return new SyncEventResult(e.EventId, SyncOutcome.Idempotent, "Already applied by EventId");

        return e.EntityType switch
        {
            AuditEntityType.Survey => await ApplySurveyEventAsync(e, ct),
            AuditEntityType.Point => await ApplyPointEventAsync(e, ct),
            // Photo events para Slice 2 — por ahora rechazados.
            AuditEntityType.Photo => new SyncEventResult(e.EventId, SyncOutcome.RejectedInvalid,
                "Photo events not supported in Slice 1"),
            _ => new SyncEventResult(e.EventId, SyncOutcome.RejectedInvalid, $"Unknown entity_type '{e.EntityType}'"),
        };
    }

    private async Task<SyncEventResult> ApplySurveyEventAsync(SyncEventDto e, CancellationToken ct)
    {
        var survey = await _db.Surveys.FirstOrDefaultAsync(s => s.Id == e.EntityId, ct);

        switch (e.EventType)
        {
            case AuditEventType.Created:
                if (survey is not null)
                    return new SyncEventResult(e.EventId, SyncOutcome.Idempotent,
                        "Survey already exists — created event is idempotent");
                // Survey creation via sync push está fuera del Slice 1
                // (se crea por POST /api/v1/surveys directamente). Rechazo limpio.
                return new SyncEventResult(e.EventId, SyncOutcome.RejectedInvalid,
                    "Survey creation must use POST /api/v1/surveys, not sync push");

            case AuditEventType.FieldUpdated when e.Field == "name" || e.Field == "description":
                if (survey is null)
                    return new SyncEventResult(e.EventId, SyncOutcome.RejectedNotFound, $"Survey {e.EntityId} not found");
                // (LWW resolution sobre Survey fields queda como ext en Slice futuro;
                //  para Slice 1 no editamos surveys via sync.)
                return new SyncEventResult(e.EventId, SyncOutcome.RejectedInvalid,
                    "Survey field updates via sync push not yet supported");

            default:
                return new SyncEventResult(e.EventId, SyncOutcome.RejectedInvalid,
                    $"Unsupported survey event_type '{e.EventType}'");
        }
    }

    private async Task<SyncEventResult> ApplyPointEventAsync(SyncEventDto e, CancellationToken ct)
    {
        return e.EventType switch
        {
            AuditEventType.Created => await ApplyPointCreatedAsync(e, ct),
            AuditEventType.FieldUpdated => await ApplyPointFieldUpdatedAsync(e, ct),
            AuditEventType.Deleted => await ApplyPointDeletedAsync(e, ct),
            AuditEventType.Restored => await ApplyPointRestoredAsync(e, ct),
            _ => new SyncEventResult(e.EventId, SyncOutcome.RejectedInvalid,
                $"Unsupported point event_type '{e.EventType}'"),
        };
    }

    private async Task<SyncEventResult> ApplyPointCreatedAsync(SyncEventDto e, CancellationToken ct)
    {
        var existing = await _db.Points.FirstOrDefaultAsync(p => p.Id == e.EntityId, ct);
        if (existing is not null)
        {
            // Punto ya creado — registramos audit como idempotente.
            await RecordAuditAsync(e, ct);
            return new SyncEventResult(e.EventId, SyncOutcome.Idempotent,
                "Point already exists — created event is idempotent");
        }

        var payload = ParseCreatePayload(e.NewValueJson);
        if (payload is null)
            return new SyncEventResult(e.EventId, SyncOutcome.RejectedInvalid,
                "created event missing or invalid new_value payload");

        // RN-08: si el survey está cerrado y el timestamp del punto es posterior al cierre → rechazado.
        var survey = await _db.Surveys.AsNoTracking().FirstOrDefaultAsync(s => s.Id == payload.SurveyId, ct);
        if (survey is null)
            return new SyncEventResult(e.EventId, SyncOutcome.RejectedNotFound, $"Survey {payload.SurveyId} not found");

        if (survey.Status == SurveyStatus.Cerrado &&
            survey.ClosedAt is not null &&
            e.TimestampOriginal > survey.ClosedAt.Value)
        {
            // US-19: registramos el conflicto pendiente para que el usuario decida
            // (descartar o reabrir el survey y aplicar). NO grabamos audit del create
            // — el evento NO sucedió contra el modelo.
            RecordConflict(survey.Id, ConflictTypes.PostClose, e,
                pointId: e.EntityId, fieldKey: null,
                attemptedValueJson: e.NewValueJson, currentValueJson: null);
            return new SyncEventResult(e.EventId, SyncOutcome.RejectedPostClose,
                $"Captura posterior al cierre del relevamiento ({survey.ClosedAt:O})");
        }

        var point = Point.Create(
            id: e.EntityId,
            surveyId: payload.SurveyId,
            latitude: payload.Latitude,
            longitude: payload.Longitude,
            accuracyM: payload.AccuracyM,
            createdBy: e.AuthorId,
            origin: e.Origin,
            captureMode: payload.CaptureMode,
            deviceId: e.DeviceId,
            createdAt: e.TimestampOriginal);

        _db.Points.Add(point);
        await RecordAuditAsync(e, ct);
        return new SyncEventResult(e.EventId, SyncOutcome.Applied, "Point created");
    }

    private async Task<SyncEventResult> ApplyPointFieldUpdatedAsync(SyncEventDto e, CancellationToken ct)
    {
        if (string.IsNullOrEmpty(e.Field))
            return new SyncEventResult(e.EventId, SyncOutcome.RejectedInvalid, "field is required for field_updated");

        var point = await _db.Points.FirstOrDefaultAsync(p => p.Id == e.EntityId, ct);
        if (point is null)
            return new SyncEventResult(e.EventId, SyncOutcome.RejectedNotFound, $"Point {e.EntityId} not found");

        var survey = await _db.Surveys.AsNoTracking().FirstOrDefaultAsync(s => s.Id == point.SurveyId, ct);
        var ownerId = survey?.OwnerId ?? Guid.Empty;
        var thisIsFromOwner = e.AuthorId == ownerId;

        // RN-07 (precedencia del dueño): si el dueño alguna vez escribió ESE campo,
        // ningún colaborador puede sobrescribirlo. Persistente, no "última escritura".
        var ownerEverWroteThisField = await _db.AuditEvents.AsNoTracking()
            .AnyAsync(a => a.EntityType == AuditEntityType.Point
                        && a.EntityId == e.EntityId
                        && a.EventType == AuditEventType.FieldUpdated
                        && a.FieldKey == e.Field
                        && a.AuthorId == ownerId, ct);

        if (ownerEverWroteThisField && !thisIsFromOwner)
        {
            await RecordAuditAsync(e, ct);
            var currentValue = await ReadCurrentFieldValueAsync(point, e.Field!, ct);
            RecordConflict(point.SurveyId, ConflictTypes.OwnerPrecedence, e,
                pointId: point.Id, fieldKey: e.Field,
                attemptedValueJson: e.NewValueJson,
                currentValueJson: currentValue);
            return new SyncEventResult(e.EventId, SyncOutcome.LosesOwnerPrecedence,
                "Owner's edit prevails (RN-07)");
        }

        if (thisIsFromOwner)
        {
            // LWW entre ediciones del mismo dueño (caso multi-device del dueño).
            var lastOwnerEdit = await _db.AuditEvents.AsNoTracking()
                .Where(a => a.EntityType == AuditEntityType.Point
                         && a.EntityId == e.EntityId
                         && a.EventType == AuditEventType.FieldUpdated
                         && a.FieldKey == e.Field
                         && a.AuthorId == ownerId)
                .OrderByDescending(a => a.TimestampOriginal)
                .FirstOrDefaultAsync(ct);
            if (lastOwnerEdit is not null && e.TimestampOriginal <= lastOwnerEdit.TimestampOriginal)
            {
                await RecordAuditAsync(e, ct);
                RecordConflict(point.SurveyId, ConflictTypes.Lww, e,
                    pointId: point.Id, fieldKey: e.Field,
                    attemptedValueJson: e.NewValueJson,
                    currentValueJson: lastOwnerEdit.NewValueJson);
                return new SyncEventResult(e.EventId, SyncOutcome.LosesLWW,
                    $"Owner already has a newer edit (last: {lastOwnerEdit.TimestampOriginal:O})");
            }

            await ApplyFieldChangeAsync(point, e, ct);
            await RecordAuditAsync(e, ct);
            return new SyncEventResult(e.EventId, SyncOutcome.Applied,
                ownerEverWroteThisField ? "Owner update" : "Owner overrides any prior collab edits (RN-07)");
        }

        // Colaborador editando, dueño nunca tocó este campo: LWW puro entre colaboradores.
        var lastCollabEdit = await _db.AuditEvents.AsNoTracking()
            .Where(a => a.EntityType == AuditEntityType.Point
                     && a.EntityId == e.EntityId
                     && a.EventType == AuditEventType.FieldUpdated
                     && a.FieldKey == e.Field)
            .OrderByDescending(a => a.TimestampOriginal)
            .FirstOrDefaultAsync(ct);
        if (lastCollabEdit is not null && e.TimestampOriginal <= lastCollabEdit.TimestampOriginal)
        {
            await RecordAuditAsync(e, ct);
            RecordConflict(point.SurveyId, ConflictTypes.Lww, e,
                pointId: point.Id, fieldKey: e.Field,
                attemptedValueJson: e.NewValueJson,
                currentValueJson: lastCollabEdit.NewValueJson);
            return new SyncEventResult(e.EventId, SyncOutcome.LosesLWW,
                $"Older timestamp than current value (last: {lastCollabEdit.TimestampOriginal:O})");
        }

        await ApplyFieldChangeAsync(point, e, ct);
        await RecordAuditAsync(e, ct);
        return new SyncEventResult(e.EventId, SyncOutcome.Applied, "Field updated by collaborator");
    }

    private async Task<SyncEventResult> ApplyPointDeletedAsync(SyncEventDto e, CancellationToken ct)
    {
        var point = await _db.Points.FirstOrDefaultAsync(p => p.Id == e.EntityId, ct);
        if (point is null)
            return new SyncEventResult(e.EventId, SyncOutcome.RejectedNotFound, $"Point {e.EntityId} not found");

        if (!point.IsDeleted)
            point.SoftDelete(e.TimestampOriginal);
        await RecordAuditAsync(e, ct);
        return new SyncEventResult(e.EventId, SyncOutcome.Applied, "Point soft-deleted");
    }

    private async Task<SyncEventResult> ApplyPointRestoredAsync(SyncEventDto e, CancellationToken ct)
    {
        var point = await _db.Points.FirstOrDefaultAsync(p => p.Id == e.EntityId, ct);
        if (point is null)
            return new SyncEventResult(e.EventId, SyncOutcome.RejectedNotFound, $"Point {e.EntityId} not found");

        if (point.IsDeleted)
            point.Restore(e.TimestampOriginal);
        await RecordAuditAsync(e, ct);
        return new SyncEventResult(e.EventId, SyncOutcome.Applied, "Point restored");
    }

    private async Task ApplyFieldChangeAsync(Point point, SyncEventDto e, CancellationToken ct)
    {
        if (e.Field is "title" or "description")
        {
            var newValue = string.IsNullOrEmpty(e.NewValueJson) ? null : e.NewValueJson.Trim('"');
            point.UpdateField(e.Field, newValue, e.TimestampOriginal);
        }
        else if (e.Field == "coords")
        {
            var coords = ParseCoordsPayload(e.NewValueJson);
            if (coords is null) throw new InvalidOperationException("Invalid coords payload");
            point.UpdateCoords(coords.Latitude, coords.Longitude, coords.AccuracyM, e.TimestampOriginal);
        }
        else
        {
            // E.5.b — Campo de plantilla custom (fecha_inspeccion, condicion_general, etc).
            // Se almacena en PointFieldValues. Upsert: una sola fila por (PointId, FieldKey).
            // El value se guarda como JSON crudo; el cliente sabe el tipo via la plantilla.
            var existing = await _db.PointFieldValues
                .FirstOrDefaultAsync(v => v.PointId == point.Id && v.FieldKey == e.Field!, ct);
            if (existing is null)
            {
                _db.PointFieldValues.Add(PointFieldValue.Create(
                    id: Guid.NewGuid(),
                    pointId: point.Id,
                    fieldKey: e.Field!,
                    valueJson: e.NewValueJson,
                    updatedBy: e.AuthorId,
                    updatedAt: e.TimestampOriginal));
            }
            else
            {
                existing.UpdateValue(e.NewValueJson, e.AuthorId, e.TimestampOriginal);
            }
        }
    }

    /// <summary>
    /// Crea un Conflict pendiente para que el usuario revise (US-19). El applier
    /// SaveChanges al final del batch — no necesita un await aquí.
    /// </summary>
    private void RecordConflict(
        Guid surveyId,
        string type,
        SyncEventDto e,
        Guid? pointId,
        string? fieldKey,
        string? attemptedValueJson,
        string? currentValueJson)
    {
        _db.Conflicts.Add(Conflict.Create(
            id: Guid.NewGuid(),
            surveyId: surveyId,
            type: type,
            eventId: e.EventId,
            pointId: pointId,
            fieldKey: fieldKey,
            authorId: e.AuthorId,
            attemptedValueJson: attemptedValueJson,
            currentValueJson: currentValueJson,
            attemptedAtUtc: e.TimestampOriginal));
    }

    /// <summary>
    /// Lee el valor actual del campo desde el modelo central (Point o PointFieldValue),
    /// para guardarlo como "current" en el Conflict — útil cuando el applier rechaza
    /// la escritura por owner_precedence: el valor que prevalece está en BD, no en
    /// el último audit del field (que podría ser del propio owner via campo built-in).
    /// </summary>
    private async Task<string?> ReadCurrentFieldValueAsync(Point point, string fieldKey, CancellationToken ct)
    {
        return fieldKey switch
        {
            "title" => System.Text.Json.JsonSerializer.Serialize(point.Title),
            "description" => System.Text.Json.JsonSerializer.Serialize(point.Description),
            "coords" => System.Text.Json.JsonSerializer.Serialize(new
            {
                latitude = point.Latitude,
                longitude = point.Longitude,
                accuracyM = point.AccuracyM,
            }),
            _ => await _db.PointFieldValues.AsNoTracking()
                .Where(v => v.PointId == point.Id && v.FieldKey == fieldKey)
                .Select(v => v.ValueJson)
                .FirstOrDefaultAsync(ct),
        };
    }

    private async Task RecordAuditAsync(SyncEventDto e, CancellationToken ct)
    {
        var audit = AuditEvent.Create(
            id: e.EventId,
            entityType: e.EntityType,
            entityId: e.EntityId,
            eventType: e.EventType,
            fieldKey: e.Field,
            oldValueJson: e.OldValueJson,
            newValueJson: e.NewValueJson,
            authorId: e.AuthorId,
            origin: e.Origin,
            deviceId: e.DeviceId,
            timestampOriginal: e.TimestampOriginal,
            appliedAt: _clock.UtcNow);
        _db.AuditEvents.Add(audit);
        await Task.CompletedTask;
    }

    private static bool Validate(SyncEventDto e, out string? error)
    {
        error = null;
        if (e.EventId == Guid.Empty) { error = "event_id required"; return false; }
        if (e.EntityId == Guid.Empty) { error = "entity_id required"; return false; }
        if (e.AuthorId == Guid.Empty) { error = "author_id required"; return false; }
        if (!AuditEntityType.IsValid(e.EntityType)) { error = $"invalid entity_type '{e.EntityType}'"; return false; }
        if (!AuditEventType.IsValid(e.EventType)) { error = $"invalid event_type '{e.EventType}'"; return false; }
        if (!AuditOrigin.IsValid(e.Origin)) { error = $"invalid origin '{e.Origin}'"; return false; }
        return true;
    }

    private static PointCreatePayload? ParseCreatePayload(string? json)
    {
        if (string.IsNullOrWhiteSpace(json)) return null;
        try
        {
            return System.Text.Json.JsonSerializer.Deserialize<PointCreatePayload>(json,
                new System.Text.Json.JsonSerializerOptions(System.Text.Json.JsonSerializerDefaults.Web));
        }
        catch { return null; }
    }

    private static CoordsPayload? ParseCoordsPayload(string? json)
    {
        if (string.IsNullOrWhiteSpace(json)) return null;
        try
        {
            return System.Text.Json.JsonSerializer.Deserialize<CoordsPayload>(json,
                new System.Text.Json.JsonSerializerOptions(System.Text.Json.JsonSerializerDefaults.Web));
        }
        catch { return null; }
    }

    private sealed record PointCreatePayload(
        Guid SurveyId,
        decimal Latitude,
        decimal Longitude,
        decimal? AccuracyM,
        string CaptureMode);

    private sealed record CoordsPayload(
        decimal Latitude,
        decimal Longitude,
        decimal? AccuracyM);
}
