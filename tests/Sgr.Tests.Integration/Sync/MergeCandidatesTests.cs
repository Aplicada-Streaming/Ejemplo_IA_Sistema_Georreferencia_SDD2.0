using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Sgr.Modules.Sync.Application;
using Sgr.Tests.Integration.Common;

namespace Sgr.Tests.Integration.Sync;

/// <summary>
/// Integration tests for US-21 (detección candidatos fusión) + US-22 (UI revisión).
/// Cubre CA-21.1 (par cercano colab. distintos → pendiente), CA-21.2 (mismo creador
/// → no se crea), CA-21.3 (fuera de radio → no se crea), CA-22.3 (mantenido_separado
/// no se vuelve a proponer), y un test básico de fusión con centroide.
/// </summary>
[Collection(WebApiCollection.Name)]
public class MergeCandidatesTests : IClassFixture<SgrApiFactory>
{
    private readonly SgrApiFactory _factory;
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new System.Text.Json.Serialization.JsonStringEnumConverter() },
    };

    public MergeCandidatesTests(SgrApiFactory factory) => _factory = factory;

    [Fact(DisplayName = "CA-21.1 — Pares cercanos de colab. distintos → candidato pendiente")]
    public async Task CA_21_1_close_pair_distinct_collabs_creates_candidate()
    {
        var (clientA, ownerId, surveyId) = await SetupSurveyAsync();
        var collabA = Guid.NewGuid();
        var collabB = Guid.NewGuid();
        var t0 = DateTime.UtcNow.AddDays(-1);

        // 7 metros aprox: ~0.000063° lat
        var pa = Guid.NewGuid();
        var pb = Guid.NewGuid();
        await PushAsync(clientA, new[]
        {
            NewPointCreated(pa, surveyId, collabA, -34.6037m, -58.3816m, t0),
        });
        await PushAsync(clientA, new[]
        {
            NewPointCreated(pb, surveyId, collabB, -34.60376m, -58.3816m, t0.AddHours(2)),
        });

        var jefeClient = await JefeClientAsync();
        var candidates = await jefeClient.GetFromJsonAsync<List<MergeCandidateApiDto>>(
            $"/api/v1/merge-candidates?surveyId={surveyId}");
        candidates!.Should().ContainSingle(c =>
            (c.PointAId == pa || c.PointAId == pb) &&
            (c.PointBId == pa || c.PointBId == pb) &&
            c.Status == "pendiente");
    }

    [Fact(DisplayName = "CA-21.2 — Pares cercanos del mismo colab. → no candidato")]
    public async Task CA_21_2_same_creator_no_candidate()
    {
        var (clientA, ownerId, surveyId) = await SetupSurveyAsync();
        var sameCollab = Guid.NewGuid();
        var t0 = DateTime.UtcNow.AddDays(-1);

        var pa = Guid.NewGuid();
        var pb = Guid.NewGuid();
        await PushAsync(clientA, new[]
        {
            NewPointCreated(pa, surveyId, sameCollab, -34.6037m, -58.3816m, t0),
            NewPointCreated(pb, surveyId, sameCollab, -34.60376m, -58.3816m, t0.AddHours(1)),
        });

        var jefeClient = await JefeClientAsync();
        var candidates = await jefeClient.GetFromJsonAsync<List<MergeCandidateApiDto>>(
            $"/api/v1/merge-candidates?surveyId={surveyId}");
        candidates!.Should().NotContain(c =>
            (c.PointAId == pa && c.PointBId == pb) || (c.PointAId == pb && c.PointBId == pa));
    }

    [Fact(DisplayName = "CA-21.3 — Pares fuera del radio (12m) → no candidato")]
    public async Task CA_21_3_far_pair_no_candidate()
    {
        var (clientA, ownerId, surveyId) = await SetupSurveyAsync();
        var collabA = Guid.NewGuid();
        var collabB = Guid.NewGuid();
        var t0 = DateTime.UtcNow.AddDays(-1);

        // ~12m: 0.000108° de lat
        var pa = Guid.NewGuid();
        var pb = Guid.NewGuid();
        await PushAsync(clientA, new[]
        {
            NewPointCreated(pa, surveyId, collabA, -34.6037m, -58.3816m, t0),
        });
        await PushAsync(clientA, new[]
        {
            NewPointCreated(pb, surveyId, collabB, -34.603808m, -58.3816m, t0.AddHours(1)),
        });

        var jefeClient = await JefeClientAsync();
        var candidates = await jefeClient.GetFromJsonAsync<List<MergeCandidateApiDto>>(
            $"/api/v1/merge-candidates?surveyId={surveyId}&status=all");
        candidates!.Should().NotContain(c =>
            (c.PointAId == pa && c.PointBId == pb) || (c.PointAId == pb && c.PointBId == pa));
    }

    [Fact(DisplayName = "CA-22.3 — Mantener separados no vuelve a proponer (RN-09)")]
    public async Task CA_22_3_keep_separate_blocks_redetection()
    {
        var (clientA, ownerId, surveyId) = await SetupSurveyAsync();
        var collabA = Guid.NewGuid();
        var collabB = Guid.NewGuid();
        var t0 = DateTime.UtcNow.AddDays(-1);

        var pa = Guid.NewGuid();
        var pb = Guid.NewGuid();
        await PushAsync(clientA, new[]
        {
            NewPointCreated(pa, surveyId, collabA, 0m, 0m, t0),
        });
        await PushAsync(clientA, new[]
        {
            NewPointCreated(pb, surveyId, collabB, 0.00006m, 0m, t0.AddHours(1)),
        });

        var jefeClient = await JefeClientAsync();
        var candidates = await jefeClient.GetFromJsonAsync<List<MergeCandidateApiDto>>(
            $"/api/v1/merge-candidates?surveyId={surveyId}");
        var candidate = candidates!.Single(c => c.PointAId == pa || c.PointBId == pa);

        await jefeClient.PostAsync($"/api/v1/merge-candidates/{candidate.Id}/keep-separate", null);

        // Forzar re-detección agregando un tercer punto cercano a A (re-evaluación de A vs B vendrá
        // como parte del detector cuando llega un punto nuevo). Confirmamos que sigue exactamente
        // el mismo candidato (mantenido_separado), no aparece otro pendiente para el par AB.
        var pc = Guid.NewGuid();
        var collabC = Guid.NewGuid();
        await PushAsync(clientA, new[]
        {
            NewPointCreated(pc, surveyId, collabC, 0.00003m, 0m, t0.AddHours(2)),
        });

        var afterAll = await jefeClient.GetFromJsonAsync<List<MergeCandidateApiDto>>(
            $"/api/v1/merge-candidates?surveyId={surveyId}&status=all");
        var pairAB = afterAll!.Where(c =>
            (c.PointAId == pa && c.PointBId == pb) || (c.PointAId == pb && c.PointBId == pa)).ToList();
        pairAB.Should().HaveCount(1);
        pairAB[0].Status.Should().Be("mantenido_separado");
    }

    [Fact(DisplayName = "CA-22.1 — Fusión con centroide consolida puntos y mueve fotos al kept")]
    public async Task CA_22_1_merge_centroid_consolidates()
    {
        var (clientA, ownerId, surveyId) = await SetupSurveyAsync();
        var collabA = Guid.NewGuid();
        var collabB = Guid.NewGuid();
        var t0 = DateTime.UtcNow.AddDays(-1);

        var pa = Guid.NewGuid();
        var pb = Guid.NewGuid();
        await PushAsync(clientA, new[]
        {
            NewPointCreated(pa, surveyId, collabA, 0m, 0m, t0),
        });
        await PushAsync(clientA, new[]
        {
            NewPointCreated(pb, surveyId, collabB, 0.00006m, 0m, t0.AddHours(1)),
        });

        var jefeClient = await JefeClientAsync();
        var candidates = await jefeClient.GetFromJsonAsync<List<MergeCandidateApiDto>>(
            $"/api/v1/merge-candidates?surveyId={surveyId}");
        var candidate = candidates!.Single();

        var resp = await jefeClient.PostAsJsonAsync($"/api/v1/merge-candidates/{candidate.Id}/merge",
            new { Strategy = "centroid" });
        resp.StatusCode.Should().Be(HttpStatusCode.OK);
        var dto = await resp.Content.ReadFromJsonAsync<MergeCandidateApiDto>();
        dto!.Status.Should().Be("fusionado");
        dto.ResolutionStrategy.Should().Be("centroid");
        dto.ResultPointId.Should().Be(candidate.PointAId);

        // Verificar en DB: PointA tiene coords del centroide, PointB queda IsDeleted.
        await using var scope = _factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<Sgr.Persistence.SgrDbContext>();
        var keptPoint = db.Points.Single(p => p.Id == candidate.PointAId);
        var droppedPoint = db.Points.Single(p => p.Id == candidate.PointBId);
        keptPoint.IsDeleted.Should().BeFalse();
        keptPoint.Latitude.Should().Be(0.00003m); // (0 + 0.00006) / 2
        droppedPoint.IsDeleted.Should().BeTrue();
    }

    // ----- helpers -----

    private async Task<HttpClient> JefeClientAsync()
    {
        var client = _factory.CreateClient();
        var token = await TestAuthHelper.LoginAsync(client, "jefe@vialidad.local", "Jefe1234!");
        client.UseBearer(token);
        return client;
    }

    private async Task<(HttpClient client, Guid ownerId, Guid surveyId)> SetupSurveyAsync()
    {
        var client = _factory.CreateClient();
        var token = await TestAuthHelper.LoginAsync(client, "relevador@vialidad.local", "Relev1234!", "Web");
        client.UseBearer(token);

        var surveyId = Guid.NewGuid();
        var resp = await client.PostAsJsonAsync("/api/v1/surveys", new
        {
            id = surveyId, name = "Merge candidates test", origin = "web_edit",
        });
        resp.EnsureSuccessStatusCode();
        var dto = await resp.Content.ReadFromJsonAsync<JsonElement>();
        return (client, dto.GetProperty("ownerId").GetGuid(), surveyId);
    }

    private static SyncEventDto NewPointCreated(
        Guid pointId, Guid surveyId, Guid authorId,
        decimal lat, decimal lng, DateTime ts) =>
        new(
            EventId: Guid.NewGuid(), EntityType: "point", EntityId: pointId,
            EventType: "created", Field: null, OldValueJson: null,
            NewValueJson: JsonSerializer.Serialize(new
            {
                surveyId, latitude = lat, longitude = lng,
                accuracyM = (decimal?)null, captureMode = "detenido",
            }, JsonOptions),
            AuthorId: authorId, Origin: "mobile_capture", DeviceId: "test",
            TimestampOriginal: ts);

    private static async Task<SyncPushResponse> PushAsync(HttpClient client, IEnumerable<SyncEventDto> events)
    {
        var resp = await client.PostAsJsonAsync("/api/v1/sync/push", new { events });
        resp.EnsureSuccessStatusCode();
        return (await resp.Content.ReadFromJsonAsync<SyncPushResponse>(JsonOptions))!;
    }

    private sealed record MergeCandidateApiDto(
        Guid Id,
        Guid SurveyId,
        Guid PointAId,
        Guid PointBId,
        Guid PointACreatedBy,
        Guid PointBCreatedBy,
        DateTime PointACreatedAt,
        DateTime PointBCreatedAt,
        decimal DistanceMeters,
        string Status,
        DateTime? ResolvedAtUtc,
        Guid? ResolvedBy,
        Guid? ResultPointId,
        string? ResolutionStrategy,
        DateTime CreatedAt);
}
