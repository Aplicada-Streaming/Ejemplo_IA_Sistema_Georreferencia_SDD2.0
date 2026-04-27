using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Sgr.Domain.Audit;
using Sgr.Domain.Points;
using Sgr.Domain.Surveys;
using Sgr.Modules.Sync.Application;
using Sgr.Tests.Unit.Common;

namespace Sgr.Tests.Unit.Sync;

/// <summary>
/// Tests del path de campos custom (E.5.b): cuando llega un FieldUpdated cuya
/// FieldKey no es title/description/coords, EventApplier debe upsertear en
/// PointFieldValues respetando LWW + RN-07.
/// </summary>
public class EventApplierCustomFieldsTests
{
    private readonly DateTime _t0 = new(2026, 4, 27, 10, 0, 0, DateTimeKind.Utc);

    /// <summary>Setup mínimo: un survey + un punto creado por <paramref name="ownerId"/>.</summary>
    private (EventApplier applier, Persistence.SgrDbContext db, Guid ownerId, Guid pointId)
        Setup()
    {
        var db = TestDb.CreateContext();
        var clock = new FakeDateTimeProvider(_t0.AddHours(1));
        var ownerId = Guid.NewGuid();

        var survey = Survey.Create(Guid.NewGuid(), "S", null, Guid.NewGuid(),
            ownerId, Guid.NewGuid(), null, _t0);
        db.Surveys.Add(survey);

        var point = Point.Create(Guid.NewGuid(), survey.Id, -31.7m, -60.5m, 50m,
            ownerId, AuditOrigin.MobileCapture, CaptureModes.Detenido, null, _t0);
        db.Points.Add(point);

        db.SaveChanges();
        return (new EventApplier(db, clock), db, ownerId, point.Id);
    }

    private SyncEventDto FieldEvent(Guid pointId, Guid authorId, string fieldKey, string valueJson, DateTime ts) =>
        new(
            EventId: Guid.NewGuid(),
            EntityType: AuditEntityType.Point,
            EntityId: pointId,
            EventType: AuditEventType.FieldUpdated,
            Field: fieldKey,
            OldValueJson: null,
            NewValueJson: valueJson,
            AuthorId: authorId,
            Origin: AuditOrigin.MobileCapture,
            DeviceId: null,
            TimestampOriginal: ts);

    [Fact]
    public async Task First_write_creates_PointFieldValue_row()
    {
        var (applier, db, ownerId, pointId) = Setup();

        var ev = FieldEvent(pointId, ownerId, "fecha_inspeccion", "\"2026-04-27\"", _t0.AddMinutes(5));
        var resp = await applier.ApplyAsync(new[] { ev });

        resp.Applied.Should().Be(1);
        var rows = await db.PointFieldValues.Where(v => v.PointId == pointId).ToListAsync();
        rows.Should().HaveCount(1);
        rows[0].FieldKey.Should().Be("fecha_inspeccion");
        rows[0].ValueJson.Should().Be("\"2026-04-27\"");
        rows[0].UpdatedBy.Should().Be(ownerId);
    }

    [Fact]
    public async Task Second_write_same_field_updates_same_row()
    {
        var (applier, db, ownerId, pointId) = Setup();

        await applier.ApplyAsync(new[]
        {
            FieldEvent(pointId, ownerId, "condicion_general", "\"Buena\"", _t0.AddMinutes(5)),
        });
        await applier.ApplyAsync(new[]
        {
            FieldEvent(pointId, ownerId, "condicion_general", "\"Mala\"", _t0.AddMinutes(10)),
        });

        var rows = await db.PointFieldValues
            .Where(v => v.PointId == pointId && v.FieldKey == "condicion_general")
            .ToListAsync();
        rows.Should().HaveCount(1);             // upsert, no append
        rows[0].ValueJson.Should().Be("\"Mala\"");
    }

    [Fact]
    public async Task Different_fields_create_separate_rows()
    {
        var (applier, db, ownerId, pointId) = Setup();

        await applier.ApplyAsync(new[]
        {
            FieldEvent(pointId, ownerId, "k1", "\"v1\"", _t0.AddMinutes(1)),
            FieldEvent(pointId, ownerId, "k2", "\"v2\"", _t0.AddMinutes(2)),
            FieldEvent(pointId, ownerId, "k3", "\"v3\"", _t0.AddMinutes(3)),
        });

        var rows = await db.PointFieldValues
            .Where(v => v.PointId == pointId)
            .OrderBy(v => v.FieldKey)
            .ToListAsync();
        rows.Select(r => r.FieldKey).Should().BeEquivalentTo(new[] { "k1", "k2", "k3" });
    }

    [Fact]
    public async Task Older_collab_write_after_owner_loses_RN_07()
    {
        var (applier, db, ownerId, pointId) = Setup();
        var collab = Guid.NewGuid();

        // Owner escribe primero (timestamp más reciente)
        await applier.ApplyAsync(new[]
        {
            FieldEvent(pointId, ownerId, "prioridad", "\"Alta\"", _t0.AddMinutes(10)),
        });

        // Colaborador llega tarde con timestamp anterior — RN-07: pierde aunque su evento sea posterior
        var collabEvent = FieldEvent(pointId, collab, "prioridad", "\"Baja\"", _t0.AddMinutes(20));
        var resp = await applier.ApplyAsync(new[] { collabEvent });

        // El evento del colaborador es POSTERIOR pero el dueño ya escribió ese campo:
        // RN-07 dice que el dueño siempre prevalece, así que se rechaza con LosesOwnerPrecedence.
        resp.Results.Single().Outcome.Should().Be(SyncOutcome.LosesOwnerPrecedence);

        var row = await db.PointFieldValues.SingleAsync(v => v.PointId == pointId && v.FieldKey == "prioridad");
        row.ValueJson.Should().Be("\"Alta\"");      // sigue el del owner
        row.UpdatedBy.Should().Be(ownerId);
    }

    [Fact]
    public async Task Owner_can_overwrite_own_previous_field_value()
    {
        var (applier, db, ownerId, pointId) = Setup();

        await applier.ApplyAsync(new[]
        {
            FieldEvent(pointId, ownerId, "obs", "\"v1\"", _t0.AddMinutes(5)),
        });
        await applier.ApplyAsync(new[]
        {
            FieldEvent(pointId, ownerId, "obs", "\"v2\"", _t0.AddMinutes(10)),
        });

        var row = await db.PointFieldValues.SingleAsync(v => v.PointId == pointId && v.FieldKey == "obs");
        row.ValueJson.Should().Be("\"v2\"");
    }

    [Fact]
    public async Task Owner_older_event_loses_LWW_to_owner_newer()
    {
        var (applier, db, ownerId, pointId) = Setup();

        // Aplicamos primero el reciente
        await applier.ApplyAsync(new[]
        {
            FieldEvent(pointId, ownerId, "obs", "\"reciente\"", _t0.AddMinutes(20)),
        });
        // Después llega uno más viejo (otro device del mismo dueño quedó offline más tiempo)
        var stale = FieldEvent(pointId, ownerId, "obs", "\"viejo\"", _t0.AddMinutes(5));
        var resp = await applier.ApplyAsync(new[] { stale });

        resp.Results.Single().Outcome.Should().Be(SyncOutcome.LosesLWW);

        var row = await db.PointFieldValues.SingleAsync(v => v.PointId == pointId && v.FieldKey == "obs");
        row.ValueJson.Should().Be("\"reciente\"");
    }

    [Fact]
    public async Task Idempotent_replay_same_event_does_not_duplicate()
    {
        var (applier, db, ownerId, pointId) = Setup();

        var ev = FieldEvent(pointId, ownerId, "k", "\"x\"", _t0.AddMinutes(5));
        await applier.ApplyAsync(new[] { ev });
        var resp = await applier.ApplyAsync(new[] { ev });   // mismo EventId

        resp.Results.Single().Outcome.Should().Be(SyncOutcome.Idempotent);
        var rows = await db.PointFieldValues.Where(v => v.PointId == pointId).CountAsync();
        rows.Should().Be(1);
    }

    [Fact]
    public async Task Field_updated_emits_audit_event()
    {
        var (applier, db, ownerId, pointId) = Setup();

        await applier.ApplyAsync(new[]
        {
            FieldEvent(pointId, ownerId, "fecha_inspeccion", "\"2026-04-27\"", _t0.AddMinutes(5)),
        });

        var audits = await db.AuditEvents
            .Where(a => a.EntityType == AuditEntityType.Point
                     && a.EntityId == pointId
                     && a.FieldKey == "fecha_inspeccion")
            .ToListAsync();
        audits.Should().HaveCount(1);
        audits[0].EventType.Should().Be(AuditEventType.FieldUpdated);
        audits[0].AuthorId.Should().Be(ownerId);
    }
}
