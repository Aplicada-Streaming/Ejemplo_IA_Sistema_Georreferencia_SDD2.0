using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Sgr.Modules.Sync.Application;
using Sgr.Tests.Integration.Common;

namespace Sgr.Tests.Integration.Sync;

/// <summary>
/// Integration tests for US-05 / CU-08 (pull diferencial).
/// Cubre el demo del Sprint Review: dos relevadores trabajando offline, después sync,
/// y se ven mutuamente.
/// </summary>
[Collection(WebApiCollection.Name)]
public class PullDiffTests : IClassFixture<SgrApiFactory>
{
    private readonly SgrApiFactory _factory;
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new System.Text.Json.Serialization.JsonStringEnumConverter() },
    };

    public PullDiffTests(SgrApiFactory factory) => _factory = factory;

    [Fact(DisplayName = "CA-5.1 — pull con since=null devuelve todos los eventos del survey")]
    public async Task Pull_with_no_since_returns_all_events()
    {
        var (clientA, ownerId, surveyId) = await SetupOwnedSurveyAsync();

        var t0 = DateTime.UtcNow.AddMinutes(-10);
        await PushPointCreatedAsync(clientA, surveyId, ownerId, t0);
        await PushPointCreatedAsync(clientA, surveyId, ownerId, t0.AddMinutes(1));
        await PushPointCreatedAsync(clientA, surveyId, ownerId, t0.AddMinutes(2));

        var resp = await PullAsync(clientA, surveyId);
        resp.Items.Should().HaveCount(4); // 1 created del survey + 3 created de points
        resp.Items.Last().AppliedAt.Should().BeOnOrAfter(t0);
        resp.NextSince.Should().NotBeNull();
        resp.HasMore.Should().BeFalse();
    }

    [Fact(DisplayName = "CA-5.2 — pull con since posterior solo devuelve eventos posteriores")]
    public async Task Pull_with_since_returns_only_newer_events()
    {
        var (clientA, ownerId, surveyId) = await SetupOwnedSurveyAsync();

        await PushPointCreatedAsync(clientA, surveyId, ownerId, DateTime.UtcNow.AddMinutes(-5));

        // Pull todo, capturar nextSince.
        var first = await PullAsync(clientA, surveyId);
        var checkpoint = first.NextSince!.Value;
        first.Items.Should().NotBeEmpty();

        // Crear 2 eventos más después del checkpoint.
        await PushPointCreatedAsync(clientA, surveyId, ownerId, DateTime.UtcNow);
        await PushPointCreatedAsync(clientA, surveyId, ownerId, DateTime.UtcNow.AddSeconds(1));

        // Pull con since = checkpoint anterior.
        var second = await PullAsync(clientA, surveyId, checkpoint);
        second.Items.Should().HaveCount(2);
        second.Items.Should().OnlyContain(i => i.AppliedAt > checkpoint);
    }

    [Fact(DisplayName = "CA-5.3 — eventos en orden cronológico (AppliedAt ASC)")]
    public async Task Pull_returns_events_in_chronological_order()
    {
        var (clientA, ownerId, surveyId) = await SetupOwnedSurveyAsync();

        var t0 = DateTime.UtcNow;
        // Push en orden inverso al timestamp original — el applier debe aplicarlos correctamente.
        await PushPointCreatedAsync(clientA, surveyId, ownerId, t0.AddMinutes(2));
        await PushPointCreatedAsync(clientA, surveyId, ownerId, t0);
        await PushPointCreatedAsync(clientA, surveyId, ownerId, t0.AddMinutes(1));

        var resp = await PullAsync(clientA, surveyId);
        var appliedAts = resp.Items.Select(i => i.AppliedAt).ToList();
        appliedAts.Should().BeInAscendingOrder();
    }

    [Fact(DisplayName = "Demo Sprint Review — dos relevadores se ven mutuamente tras sync")]
    public async Task Two_collaborators_see_each_other_after_sync()
    {
        // Setup: relevador (dueño) crea survey, jefe agrega un colaborador adicional.
        // Para Slice 1 simplificamos: el segundo relevador push con un AuthorId distinto.
        var (clientA, ownerId, surveyId) = await SetupOwnedSurveyAsync();
        var collaboratorId = Guid.NewGuid();

        var t0 = DateTime.UtcNow.AddMinutes(-5);

        // Cada relevador, offline, crea 2 puntos.
        await PushPointCreatedAsync(clientA, surveyId, ownerId, t0, lat: -34.60m, lng: -58.38m);
        await PushPointCreatedAsync(clientA, surveyId, ownerId, t0.AddMinutes(1), lat: -34.61m, lng: -58.39m);
        await PushPointCreatedAsync(clientA, surveyId, collaboratorId, t0.AddMinutes(2), lat: -34.62m, lng: -58.40m);
        await PushPointCreatedAsync(clientA, surveyId, collaboratorId, t0.AddMinutes(3), lat: -34.63m, lng: -58.41m);

        // Cliente B (dueño) hace pull desde cero — debe ver los 4 puntos creados, los 2 propios y los 2 del colaborador.
        var pullDueno = await PullAsync(clientA, surveyId);
        var pointEvents = pullDueno.Items.Where(i => i.EntityType == "point" && i.EventType == "created").ToList();
        pointEvents.Should().HaveCount(4);
        pointEvents.Where(p => p.AuthorId == ownerId).Should().HaveCount(2);
        pointEvents.Where(p => p.AuthorId == collaboratorId).Should().HaveCount(2);
    }

    [Fact(DisplayName = "Pull respeta paginación (max param)")]
    public async Task Pull_paginates_with_max_param()
    {
        var (clientA, ownerId, surveyId) = await SetupOwnedSurveyAsync();

        var t0 = DateTime.UtcNow.AddMinutes(-1);
        for (int i = 0; i < 5; i++)
            await PushPointCreatedAsync(clientA, surveyId, ownerId, t0.AddSeconds(i));

        var firstPage = await PullAsync(clientA, surveyId, since: null, max: 3);
        firstPage.Items.Should().HaveCount(3);
        firstPage.HasMore.Should().BeTrue();
        firstPage.NextSince.Should().NotBeNull();

        var secondPage = await PullAsync(clientA, surveyId, since: firstPage.NextSince, max: 3);
        secondPage.Items.Should().NotBeEmpty();
        secondPage.HasMore.Should().BeFalse();
    }

    [Fact(DisplayName = "Pull con surveyId inexistente devuelve 404")]
    public async Task Pull_with_unknown_survey_returns_404()
    {
        var (clientA, _, _) = await SetupOwnedSurveyAsync();

        var resp = await clientA.GetAsync($"/api/v1/surveys/{Guid.NewGuid()}/changes");
        resp.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact(DisplayName = "Pull sin auth devuelve 401")]
    public async Task Pull_without_token_returns_401()
    {
        var client = _factory.CreateClient();
        var resp = await client.GetAsync($"/api/v1/surveys/{Guid.NewGuid()}/changes");
        resp.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ------------- Helpers -------------

    private async Task<(HttpClient client, Guid ownerId, Guid surveyId)> SetupOwnedSurveyAsync()
    {
        var client = _factory.CreateClient();
        var token = await TestAuthHelper.LoginAsync(client, "relevador@vialidad.local", "Relev1234!", "Web");
        client.UseBearer(token);

        var surveyId = Guid.NewGuid();
        var resp = await client.PostAsJsonAsync("/api/v1/surveys", new
        {
            id = surveyId,
            name = "Pull diff test",
            origin = "web_edit",
        });
        resp.EnsureSuccessStatusCode();
        var dto = await resp.Content.ReadFromJsonAsync<JsonElement>();
        return (client, dto.GetProperty("ownerId").GetGuid(), surveyId);
    }

    private static async Task PushPointCreatedAsync(
        HttpClient client, Guid surveyId, Guid authorId, DateTime ts,
        decimal lat = -34.6m, decimal lng = -58.4m)
    {
        var ev = new SyncEventDto(
            EventId: Guid.NewGuid(),
            EntityType: "point",
            EntityId: Guid.NewGuid(),
            EventType: "created",
            Field: null,
            OldValueJson: null,
            NewValueJson: JsonSerializer.Serialize(new
            {
                surveyId,
                latitude = lat,
                longitude = lng,
                accuracyM = (decimal?)null,
                captureMode = "detenido",
            }, JsonOptions),
            AuthorId: authorId,
            Origin: "mobile_capture",
            DeviceId: "device-test",
            TimestampOriginal: ts);
        var resp = await client.PostAsJsonAsync("/api/v1/sync/push", new { events = new[] { ev } });
        resp.EnsureSuccessStatusCode();
    }

    private static async Task<PullDiffResponse> PullAsync(
        HttpClient client, Guid surveyId, DateTime? since = null, int? max = null)
    {
        var qs = new List<string>();
        if (since is not null) qs.Add($"since={Uri.EscapeDataString(since.Value.ToString("O"))}");
        if (max is not null) qs.Add($"max={max}");
        var url = $"/api/v1/surveys/{surveyId}/changes" + (qs.Count > 0 ? "?" + string.Join("&", qs) : "");
        var resp = await client.GetAsync(url);
        resp.EnsureSuccessStatusCode();
        return (await resp.Content.ReadFromJsonAsync<PullDiffResponse>(JsonOptions))!;
    }
}
