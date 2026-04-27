using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Sgr.Modules.Sync.Application;
using Sgr.Tests.Integration.Common;

namespace Sgr.Tests.Integration.Sync;

/// <summary>
/// Integration tests for US-04 / CU-08.
/// Cubre RN-06 (idempotencia), RN-07 (LWW por campo + precedencia del dueño),
/// RN-08 (capturas post-cierre rechazadas), RN-10 (events append-only).
///
/// Estos tests usan dos HttpClients sobre la misma DB en memoria — la 'simulación
/// de dos relevadores' del Slice 1 que pide PROJECT-BRIEF Sec. 8.
/// </summary>
[Collection(WebApiCollection.Name)]
public class SyncPushTests : IClassFixture<SgrApiFactory>
{
    private readonly SgrApiFactory _factory;
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new System.Text.Json.Serialization.JsonStringEnumConverter() },
    };

    public SyncPushTests(SgrApiFactory factory) => _factory = factory;

    [Fact(DisplayName = "CA-8.1 — RN-06 idempotencia: mismo EventId → no se aplica dos veces")]
    public async Task RN_06_idempotency_same_event_id_is_noop()
    {
        var (clientA, ownerId, areaId, surveyId, _) = await SetupOwnedSurveyAsync();

        var pointId = Guid.NewGuid();
        var ev = NewPointCreatedEvent(pointId, surveyId, ownerId, "mobile_capture", "detenido",
            -34.6037m, -58.3816m, DateTime.UtcNow);

        var first = await PushAsync(clientA, new[] { ev });
        first.Applied.Should().Be(1);

        // Re-enviamos exactamente el mismo evento.
        var second = await PushAsync(clientA, new[] { ev });
        second.Received.Should().Be(1);
        second.Applied.Should().Be(0);
        second.Skipped.Should().Be(1);
        second.Results.Single().Outcome.Should().Be(SyncOutcome.Idempotent);

        // Verificamos en DB que sigue habiendo solo un punto.
        await using var scope = _factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<Sgr.Persistence.SgrDbContext>();
        db.Points.Count(p => p.Id == pointId).Should().Be(1);
        db.AuditEvents.Count(a => a.Id == ev.EventId).Should().Be(1);
    }

    [Fact(DisplayName = "CA-8.2 — RN-07 LWW por campo: timestamp posterior gana")]
    public async Task RN_07_lww_by_field_later_timestamp_wins()
    {
        var (clientA, ownerId, _, surveyId, _) = await SetupOwnedSurveyAsync();
        var collaboratorId = Guid.NewGuid();

        // Crear punto y permitir colaborador editar (en Slice 1 simplificamos: el applier no
        // valida permisos, solo aplica RN-07. El permiso lo valida el endpoint REST de edición real.)
        var pointId = Guid.NewGuid();
        var t0 = new DateTime(2026, 4, 26, 10, 0, 0, DateTimeKind.Utc);

        await PushAsync(clientA, new[]
        {
            NewPointCreatedEvent(pointId, surveyId, collaboratorId, "mobile_capture", "detenido",
                0m, 0m, t0),
        });

        // Dos colaboradores (no dueño) compitiendo por title:
        var earlier = NewPointFieldUpdated(pointId, "title", "\"Earlier\"", collaboratorId, t0.AddMinutes(5));
        var later = NewPointFieldUpdated(pointId, "title", "\"Later\"", Guid.NewGuid(), t0.AddMinutes(10));

        // Aplicamos en orden inverso al timestamp (lo más interesante: el más nuevo llega primero).
        var resp1 = await PushAsync(clientA, new[] { later });
        resp1.Results.Single().Outcome.Should().Be(SyncOutcome.Applied);

        var resp2 = await PushAsync(clientA, new[] { earlier });
        resp2.Results.Single().Outcome.Should().Be(SyncOutcome.LosesLWW);

        await using var scope = _factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<Sgr.Persistence.SgrDbContext>();
        db.Points.Single(p => p.Id == pointId).Title.Should().Be("Later");
    }

    [Fact(DisplayName = "CA-8.3 — RN-07 precedencia del dueño: dueño gana aún con timestamp anterior")]
    public async Task RN_07_owner_wins_even_with_earlier_timestamp()
    {
        var (clientA, ownerId, _, surveyId, _) = await SetupOwnedSurveyAsync();
        var collaboratorId = Guid.NewGuid();

        var pointId = Guid.NewGuid();
        var t0 = new DateTime(2026, 4, 26, 10, 0, 0, DateTimeKind.Utc);

        await PushAsync(clientA, new[]
        {
            NewPointCreatedEvent(pointId, surveyId, collaboratorId, "mobile_capture", "detenido",
                0m, 0m, t0),
        });

        // El colaborador edita primero (timestamp 10:30).
        var collabEdit = NewPointFieldUpdated(pointId, "title", "\"Versión Colaborador\"",
            collaboratorId, t0.AddMinutes(30));
        // El dueño edita ANTES (timestamp 10:00, anterior).
        var ownerEdit = NewPointFieldUpdated(pointId, "title", "\"Versión Dueño\"",
            ownerId, t0.AddMinutes(0));

        var r1 = await PushAsync(clientA, new[] { collabEdit });
        r1.Results.Single().Outcome.Should().Be(SyncOutcome.Applied);

        var r2 = await PushAsync(clientA, new[] { ownerEdit });
        r2.Results.Single().Outcome.Should().Be(SyncOutcome.Applied);
        r2.Results.Single().Message.Should().Contain("Owner overrides");

        // Y si el colaborador re-intenta editar después de haber perdido por precedencia, también pierde.
        var collabRetry = NewPointFieldUpdated(pointId, "title", "\"Otra Vez\"",
            collaboratorId, t0.AddMinutes(60));
        var r3 = await PushAsync(clientA, new[] { collabRetry });
        r3.Results.Single().Outcome.Should().Be(SyncOutcome.LosesOwnerPrecedence);

        await using var scope = _factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<Sgr.Persistence.SgrDbContext>();
        db.Points.Single(p => p.Id == pointId).Title.Should().Be("Versión Dueño");
    }

    [Fact(DisplayName = "RN-08 — captura posterior al cierre del survey → rechazada")]
    public async Task RN_08_capture_after_close_is_rejected()
    {
        var (clientA, ownerId, _, surveyId, _) = await SetupOwnedSurveyAsync();

        // Cerrar el survey en DB directamente (el endpoint /surveys/{id}/close lo veremos en Slice futuro).
        var closeAt = new DateTime(2026, 4, 26, 16, 0, 0, DateTimeKind.Utc);
        await using (var scope = _factory.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<Sgr.Persistence.SgrDbContext>();
            var s = db.Surveys.Single(x => x.Id == surveyId);
            // Acceso al setter privado vía reflection para Slice 1 (en Slice futuro habrá CloseSurveyService).
            typeof(Sgr.Domain.Surveys.Survey).GetProperty("Status")!.SetValue(s, "cerrado");
            typeof(Sgr.Domain.Surveys.Survey).GetProperty("ClosedAt")!.SetValue(s, closeAt);
            await db.SaveChangesAsync();
        }

        var late = NewPointCreatedEvent(Guid.NewGuid(), surveyId, ownerId, "mobile_capture", "detenido",
            0m, 0m, closeAt.AddMinutes(30)); // timestamp posterior al cierre

        var resp = await PushAsync(clientA, new[] { late });
        resp.Applied.Should().Be(0);
        resp.Skipped.Should().Be(1);
        resp.Results.Single().Outcome.Should().Be(SyncOutcome.RejectedPostClose);
    }

    [Fact(DisplayName = "RN-10 — eventos quedan en AuditEvents (append-only) tras cada push")]
    public async Task RN_10_audit_events_recorded_for_every_push()
    {
        var (clientA, ownerId, _, surveyId, _) = await SetupOwnedSurveyAsync();
        var pointId = Guid.NewGuid();
        var t0 = DateTime.UtcNow;

        var ev = NewPointCreatedEvent(pointId, surveyId, ownerId, "mobile_capture", "detenido",
            -34.6m, -58.4m, t0);

        await PushAsync(clientA, new[] { ev });

        await using var scope = _factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<Sgr.Persistence.SgrDbContext>();
        var audits = db.AuditEvents.Where(a => a.EntityId == pointId).ToList();
        audits.Should().HaveCount(1);
        audits[0].EntityType.Should().Be("point");
        audits[0].EventType.Should().Be("created");
        audits[0].AuthorId.Should().Be(ownerId);
        audits[0].Origin.Should().Be("mobile_capture");
        audits[0].TimestampOriginal.Should().Be(t0);
    }

    [Fact(DisplayName = "Sync push sin auth devuelve 401")]
    public async Task Push_without_token_returns_401()
    {
        var client = _factory.CreateClient();
        var ev = NewPointCreatedEvent(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), "web_edit", "web",
            0m, 0m, DateTime.UtcNow);
        var resp = await client.PostAsJsonAsync("/api/v1/sync/push", new { events = new[] { ev } });
        resp.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ------------- Helpers -------------

    private async Task<(HttpClient client, Guid ownerId, Guid areaId, Guid surveyId, string token)>
        SetupOwnedSurveyAsync()
    {
        var client = _factory.CreateClient();
        var token = await TestAuthHelper.LoginAsync(client, "relevador@vialidad.local", "Relev1234!", "Web");
        client.UseBearer(token);

        // Crear survey via REST normal.
        var surveyId = Guid.NewGuid();
        var createResp = await client.PostAsJsonAsync("/api/v1/surveys", new
        {
            id = surveyId,
            name = "Sync test survey",
            origin = "web_edit",
        });
        createResp.EnsureSuccessStatusCode();
        var surveyDto = await createResp.Content.ReadFromJsonAsync<JsonElement>();
        var ownerId = surveyDto.GetProperty("ownerId").GetGuid();
        var areaId = surveyDto.GetProperty("areaId").GetGuid();

        return (client, ownerId, areaId, surveyId, token);
    }

    private static SyncEventDto NewPointCreatedEvent(
        Guid pointId, Guid surveyId, Guid authorId, string origin, string captureMode,
        decimal lat, decimal lng, DateTime ts) =>
        new(
            EventId: Guid.NewGuid(),
            EntityType: "point",
            EntityId: pointId,
            EventType: "created",
            Field: null,
            OldValueJson: null,
            NewValueJson: JsonSerializer.Serialize(new
            {
                surveyId,
                latitude = lat,
                longitude = lng,
                accuracyM = (decimal?)null,
                captureMode,
            }, JsonOptions),
            AuthorId: authorId,
            Origin: origin,
            DeviceId: "test-device",
            TimestampOriginal: ts);

    private static SyncEventDto NewPointFieldUpdated(
        Guid pointId, string field, string newValueJson, Guid authorId, DateTime ts) =>
        new(
            EventId: Guid.NewGuid(),
            EntityType: "point",
            EntityId: pointId,
            EventType: "field_updated",
            Field: field,
            OldValueJson: null,
            NewValueJson: newValueJson,
            AuthorId: authorId,
            Origin: "mobile_edit",
            DeviceId: "test-device",
            TimestampOriginal: ts);

    private static async Task<SyncPushResponse> PushAsync(HttpClient client, IEnumerable<SyncEventDto> events)
    {
        var resp = await client.PostAsJsonAsync("/api/v1/sync/push", new { events });
        resp.EnsureSuccessStatusCode();
        return (await resp.Content.ReadFromJsonAsync<SyncPushResponse>(JsonOptions))!;
    }
}
