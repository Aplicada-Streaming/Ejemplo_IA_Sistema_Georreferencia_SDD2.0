using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Sgr.Modules.Sync.Application;
using Sgr.Tests.Integration.Common;

namespace Sgr.Tests.Integration.Sync;

/// <summary>
/// Integration tests for US-19 (panel de conflictos) + US-20 (merge manual).
/// Cubre CA-19.1 (LWW resuelto auto + revert), CA-19.4 (filtro por survey),
/// CA-20.2 (revert genera nuevo evento que vuelve a aplicar LWW),
/// CA-20.3 (keep_current → resuelto_sin_cambio).
/// </summary>
[Collection(WebApiCollection.Name)]
public class ConflictsTests : IClassFixture<SgrApiFactory>
{
    private readonly SgrApiFactory _factory;
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new System.Text.Json.Serialization.JsonStringEnumConverter() },
    };

    public ConflictsTests(SgrApiFactory factory) => _factory = factory;

    [Fact(DisplayName = "CA-19.1 — LWW que pierde queda como Conflict pendiente")]
    public async Task CA_19_1_lww_loser_becomes_conflict()
    {
        var (clientA, ownerId, surveyId) = await SetupOwnedSurveyAsync();
        var pointId = Guid.NewGuid();
        var t0 = new DateTime(2026, 4, 26, 10, 0, 0, DateTimeKind.Utc);

        // Crear punto.
        await PushAsync(clientA, new[]
        {
            NewPointCreated(pointId, surveyId, ownerId, "mobile_capture", "detenido", 0m, 0m, t0),
        });

        // Dos colaboradores compitiendo por title — el "earlier" pierde por LWW.
        var collab1 = Guid.NewGuid();
        var collab2 = Guid.NewGuid();
        var later = NewPointFieldUpdated(pointId, "title", "\"Later\"", collab1, t0.AddMinutes(10));
        var earlier = NewPointFieldUpdated(pointId, "title", "\"Earlier\"", collab2, t0.AddMinutes(5));

        await PushAsync(clientA, new[] { later });
        var resp = await PushAsync(clientA, new[] { earlier });
        resp.Results.Single().Outcome.Should().Be(SyncOutcome.LosesLWW);

        // Login como jefe (rol que puede ver conflicts).
        var jefeClient = _factory.CreateClient();
        var jefeToken = await TestAuthHelper.LoginAsync(jefeClient, "jefe@vialidad.local", "Jefe1234!");
        jefeClient.UseBearer(jefeToken);

        var conflicts = await jefeClient.GetFromJsonAsync<List<ConflictApiDto>>(
            $"/api/v1/conflicts?surveyId={surveyId}");
        conflicts.Should().NotBeNull();
        var lwwConflict = conflicts!.SingleOrDefault(c => c.Type == "lww" && c.EventId == earlier.EventId);
        lwwConflict.Should().NotBeNull();
        lwwConflict!.Status.Should().Be("pendiente");
        lwwConflict.AttemptedValueJson.Should().Contain("Earlier");
        lwwConflict.CurrentValueJson.Should().Contain("Later");
    }

    [Fact(DisplayName = "CA-20.3 — KeepCurrent marca el conflicto resuelto_sin_cambio")]
    public async Task CA_20_3_keep_current_resolves_without_change()
    {
        var (jefeClient, conflictId) = await CreateLwwConflictAndGetJefeClientAsync();

        var resp = await jefeClient.PostAsJsonAsync($"/api/v1/conflicts/{conflictId}/resolve", new { Action = "KeepCurrent" });
        resp.StatusCode.Should().Be(HttpStatusCode.OK);
        var dto = await resp.Content.ReadFromJsonAsync<ConflictApiDto>();
        dto!.Status.Should().Be("resuelto_sin_cambio");

        // Ya no aparece en la cola pendiente.
        var stillPending = await jefeClient.GetFromJsonAsync<List<ConflictApiDto>>(
            "/api/v1/conflicts?status=pendiente");
        stillPending!.Should().NotContain(c => c.Id == conflictId);
    }

    [Fact(DisplayName = "CA-20.2 — Revert genera nuevo evento que vuelve a aplicar y gana por LWW")]
    public async Task CA_20_2_revert_generates_new_event()
    {
        var (jefeClient, conflictId) = await CreateLwwConflictAndGetJefeClientAsync();

        var resp = await jefeClient.PostAsJsonAsync($"/api/v1/conflicts/{conflictId}/resolve", new { Action = "Revert" });
        resp.StatusCode.Should().Be(HttpStatusCode.OK);
        var dto = await resp.Content.ReadFromJsonAsync<ConflictApiDto>();
        dto!.Status.Should().Be("resuelto_revertido");

        // Verificar que el title del punto ahora es el "attempted" (el que había perdido).
        await using var scope = _factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<Sgr.Persistence.SgrDbContext>();
        var conflict = db.Conflicts.Single(c => c.Id == conflictId);
        var point = db.Points.Single(p => p.Id == conflict.PointId!.Value);
        point.Title.Should().Be("Earlier");
    }

    [Fact(DisplayName = "Filtro por type devuelve solo el tipo solicitado")]
    public async Task Filter_by_type_returns_subset()
    {
        var jefeClient = _factory.CreateClient();
        var jefeToken = await TestAuthHelper.LoginAsync(jefeClient, "jefe@vialidad.local", "Jefe1234!");
        jefeClient.UseBearer(jefeToken);

        var lwwOnly = await jefeClient.GetFromJsonAsync<List<ConflictApiDto>>(
            "/api/v1/conflicts?type=lww&status=all");
        lwwOnly!.Should().AllSatisfy(c => c.Type.Should().Be("lww"));
    }

    [Fact(DisplayName = "Relevador no puede acceder al panel → 403")]
    public async Task Relevador_cant_access_conflicts_panel()
    {
        var client = _factory.CreateClient();
        var token = await TestAuthHelper.LoginAsync(client, "relevador@vialidad.local", "Relev1234!", "Web");
        client.UseBearer(token);

        var resp = await client.GetAsync("/api/v1/conflicts");
        resp.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact(DisplayName = "DT-S9.1 — Revert sobre post_close reabre survey y aplica el evento")]
    public async Task DT_S9_1_post_close_revert_reopens_and_applies()
    {
        var (clientA, ownerId, surveyId) = await SetupOwnedSurveyAsync();

        // Cerrar el survey directamente en DB para que el applier rechace capturas posteriores.
        var closeAt = DateTime.UtcNow.AddDays(-2);
        await using (var scope = _factory.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<Sgr.Persistence.SgrDbContext>();
            var s = db.Surveys.Single(x => x.Id == surveyId);
            typeof(Sgr.Domain.Surveys.Survey).GetProperty("Status")!.SetValue(s, "cerrado");
            typeof(Sgr.Domain.Surveys.Survey).GetProperty("ClosedAt")!.SetValue(s, closeAt);
            await db.SaveChangesAsync();
        }

        // Capturar un punto post-cierre → genera conflict post_close.
        var pointId = Guid.NewGuid();
        var lateEvent = NewPointCreated(pointId, surveyId, ownerId, "mobile_capture", "detenido",
            -34.6m, -58.4m, closeAt.AddHours(1));
        var resp = await PushAsync(clientA, new[] { lateEvent });
        resp.Results.Single().Outcome.Should().Be(SyncOutcome.RejectedPostClose);

        // Jefe revierte el conflict → reabre survey + aplica.
        var jefeClient = _factory.CreateClient();
        var jefeToken = await TestAuthHelper.LoginAsync(jefeClient, "jefe@vialidad.local", "Jefe1234!");
        jefeClient.UseBearer(jefeToken);

        var conflicts = await jefeClient.GetFromJsonAsync<List<ConflictApiDto>>(
            $"/api/v1/conflicts?surveyId={surveyId}&type=post_close");
        var conflict = conflicts!.Single(c => c.EventId == lateEvent.EventId);

        var revertResp = await jefeClient.PostAsJsonAsync(
            $"/api/v1/conflicts/{conflict.Id}/resolve", new { Action = "Revert" });
        revertResp.StatusCode.Should().Be(HttpStatusCode.OK);
        var dto = await revertResp.Content.ReadFromJsonAsync<ConflictApiDto>();
        dto!.Status.Should().Be("resuelto_revertido");

        // Verificar: survey reabierto + punto creado.
        await using (var scope = _factory.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<Sgr.Persistence.SgrDbContext>();
            var survey = db.Surveys.Single(s => s.Id == surveyId);
            survey.Status.Should().Be("abierto");
            survey.ClosedAt.Should().BeNull();
            db.Points.Any(p => p.Id == pointId).Should().BeTrue();
        }
    }

    // ----- helpers -----

    private async Task<(HttpClient client, Guid conflictId)> CreateLwwConflictAndGetJefeClientAsync()
    {
        var (clientA, ownerId, surveyId) = await SetupOwnedSurveyAsync();
        var pointId = Guid.NewGuid();
        // t0 en el pasado para que el revert (timestamp = UtcNow al resolver) gane por LWW.
        var t0 = DateTime.UtcNow.AddDays(-1);

        await PushAsync(clientA, new[]
        {
            NewPointCreated(pointId, surveyId, ownerId, "mobile_capture", "detenido", 0m, 0m, t0),
        });

        var collab = Guid.NewGuid();
        var later = NewPointFieldUpdated(pointId, "title", "\"Later\"", collab, t0.AddMinutes(10));
        var earlier = NewPointFieldUpdated(pointId, "title", "\"Earlier\"", collab, t0.AddMinutes(5));
        await PushAsync(clientA, new[] { later });
        await PushAsync(clientA, new[] { earlier });

        var jefeClient = _factory.CreateClient();
        var jefeToken = await TestAuthHelper.LoginAsync(jefeClient, "jefe@vialidad.local", "Jefe1234!");
        jefeClient.UseBearer(jefeToken);

        var conflicts = await jefeClient.GetFromJsonAsync<List<ConflictApiDto>>(
            $"/api/v1/conflicts?surveyId={surveyId}&type=lww");
        var conflictId = conflicts!.Single(c => c.EventId == earlier.EventId).Id;
        return (jefeClient, conflictId);
    }

    private async Task<(HttpClient client, Guid ownerId, Guid surveyId)> SetupOwnedSurveyAsync()
    {
        var client = _factory.CreateClient();
        var token = await TestAuthHelper.LoginAsync(client, "relevador@vialidad.local", "Relev1234!", "Web");
        client.UseBearer(token);

        var surveyId = Guid.NewGuid();
        var resp = await client.PostAsJsonAsync("/api/v1/surveys", new
        {
            id = surveyId, name = "Conflicts test", origin = "web_edit",
        });
        resp.EnsureSuccessStatusCode();
        var dto = await resp.Content.ReadFromJsonAsync<JsonElement>();
        return (client, dto.GetProperty("ownerId").GetGuid(), surveyId);
    }

    private static SyncEventDto NewPointCreated(
        Guid pointId, Guid surveyId, Guid authorId, string origin, string captureMode,
        decimal lat, decimal lng, DateTime ts) =>
        new(
            EventId: Guid.NewGuid(), EntityType: "point", EntityId: pointId,
            EventType: "created", Field: null, OldValueJson: null,
            NewValueJson: JsonSerializer.Serialize(new
            {
                surveyId, latitude = lat, longitude = lng,
                accuracyM = (decimal?)null, captureMode,
            }, JsonOptions),
            AuthorId: authorId, Origin: origin, DeviceId: "test", TimestampOriginal: ts);

    private static SyncEventDto NewPointFieldUpdated(
        Guid pointId, string field, string newValueJson, Guid authorId, DateTime ts) =>
        new(
            EventId: Guid.NewGuid(), EntityType: "point", EntityId: pointId,
            EventType: "field_updated", Field: field, OldValueJson: null,
            NewValueJson: newValueJson,
            AuthorId: authorId, Origin: "mobile_edit", DeviceId: "test",
            TimestampOriginal: ts);

    private static async Task<SyncPushResponse> PushAsync(HttpClient client, IEnumerable<SyncEventDto> events)
    {
        var resp = await client.PostAsJsonAsync("/api/v1/sync/push", new { events });
        resp.EnsureSuccessStatusCode();
        return (await resp.Content.ReadFromJsonAsync<SyncPushResponse>(JsonOptions))!;
    }

    private sealed record ConflictApiDto(
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
}
