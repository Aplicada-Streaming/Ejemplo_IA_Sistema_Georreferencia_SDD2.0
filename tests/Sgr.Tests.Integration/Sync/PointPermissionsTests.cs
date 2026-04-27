using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Sgr.Domain.Audit;
using Sgr.Domain.Identity;
using Sgr.Domain.Points;
using Sgr.Domain.Surveys;
using Sgr.Modules.Sync.Application;
using Sgr.Persistence;
using Sgr.Tests.Integration.Common;

namespace Sgr.Tests.Integration.Sync;

/// <summary>
/// Integration tests for US-14 / RN-01 — Permisos por punto.
/// Cubre CA-14.1 a CA-14.3 sobre la edición vía sync push.
/// CA-14.4 (eliminación de relevamiento por colaborador) y CA-14.5 (UI lectura)
/// se validan a otros niveles.
/// </summary>
[Collection(WebApiCollection.Name)]
public class PointPermissionsTests : IClassFixture<SgrApiFactory>
{
    private readonly SgrApiFactory _factory;
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new System.Text.Json.Serialization.JsonStringEnumConverter() },
    };

    public PointPermissionsTests(SgrApiFactory factory) => _factory = factory;

    [Fact(DisplayName = "CA-14.1 — Colaborador edita su propio punto → Applied")]
    public async Task CA_14_1_collab_edits_own_point()
    {
        var (ownerClient, ownerId, surveyId) = await SetupSurveyAsync();
        var (collabClient, collabId) = await SeedCollaboratorAsync(ownerId, surveyId);

        // Colaborador crea su punto.
        var pointId = Guid.NewGuid();
        await SeedPointAsync(pointId, surveyId, createdBy: collabId);

        // Colaborador edita SU punto → Applied.
        var ev = NewFieldUpdated(pointId, "title", "\"Edición propia\"", collabId, DateTime.UtcNow);
        var resp = await PushAsync(collabClient, new[] { ev });
        resp.Results.Single().Outcome.Should().Be(SyncOutcome.Applied);
    }

    [Fact(DisplayName = "CA-14.2 — Colaborador edita punto de OTRO colaborador → RejectedForbidden")]
    public async Task CA_14_2_collab_cant_edit_other_collab_point()
    {
        var (ownerClient, ownerId, surveyId) = await SetupSurveyAsync();
        var (collabClient, collabId) = await SeedCollaboratorAsync(ownerId, surveyId);

        // OTRO colaborador (no logueado, sólo seedeado) crea un punto.
        var otherCollabId = Guid.NewGuid();
        var pointId = Guid.NewGuid();
        await SeedPointAsync(pointId, surveyId, createdBy: otherCollabId);

        // collabClient (logueado como un relevador del seed) intenta editar punto de otro.
        var ev = NewFieldUpdated(pointId, "title", "\"Intento robarlo\"", collabId, DateTime.UtcNow);
        var resp = await PushAsync(collabClient, new[] { ev });
        resp.Results.Single().Outcome.Should().Be(SyncOutcome.RejectedForbidden);
        resp.Applied.Should().Be(0);
    }

    [Fact(DisplayName = "CA-14.3 — Dueño del survey edita cualquier punto → Applied")]
    public async Task CA_14_3_owner_edits_any_point()
    {
        var (ownerClient, ownerId, surveyId) = await SetupSurveyAsync();

        // Un colaborador random crea un punto.
        var collabId = Guid.NewGuid();
        var pointId = Guid.NewGuid();
        await SeedPointAsync(pointId, surveyId, createdBy: collabId);

        // El dueño del survey lo edita → Applied (es owner).
        var ev = NewFieldUpdated(pointId, "title", "\"Editado por dueño\"", ownerId, DateTime.UtcNow);
        var resp = await PushAsync(ownerClient, new[] { ev });
        resp.Results.Single().Outcome.Should().Be(SyncOutcome.Applied);
    }

    [Fact(DisplayName = "CA-14 — Eliminación de punto ajeno por colaborador → RejectedForbidden")]
    public async Task Collab_cant_delete_other_collab_point()
    {
        var (ownerClient, ownerId, surveyId) = await SetupSurveyAsync();
        var (collabClient, collabId) = await SeedCollaboratorAsync(ownerId, surveyId);

        var otherCollabId = Guid.NewGuid();
        var pointId = Guid.NewGuid();
        await SeedPointAsync(pointId, surveyId, createdBy: otherCollabId);

        var del = NewEvent(pointId, AuditEventType.Deleted, field: null, valueJson: null, collabId, DateTime.UtcNow);
        var resp = await PushAsync(collabClient, new[] { del });
        resp.Results.Single().Outcome.Should().Be(SyncOutcome.RejectedForbidden);
    }

    // ---------------- helpers ----------------

    private async Task<(HttpClient client, Guid ownerId, Guid surveyId)> SetupSurveyAsync()
    {
        var client = _factory.CreateClient();
        // El "dueño" del survey en este caso es el jefe, que también lo crea via REST.
        // Login como relevador (que crea el survey y queda como owner).
        var token = await TestAuthHelper.LoginAsync(client, "relevador@vialidad.local", "Relev1234!", "Web");
        client.UseBearer(token);

        var surveyId = Guid.NewGuid();
        var resp = await client.PostAsJsonAsync("/api/v1/surveys", new
        {
            id = surveyId, name = "Permisos test", origin = "web_edit",
        });
        resp.EnsureSuccessStatusCode();
        var dto = await resp.Content.ReadFromJsonAsync<JsonElement>();
        var ownerId = dto.GetProperty("ownerId").GetGuid();
        return (client, ownerId, surveyId);
    }

    /// <summary>
    /// "Colaborador" = en este test, otro user con login válido y mismo JWT actor distinto al owner.
    /// Como el seed solo trae un relevador, fabricamos un relevador 2 directo en DB y devolvemos
    /// su HttpClient con login real.
    /// </summary>
    private async Task<(HttpClient client, Guid userId)> SeedCollaboratorAsync(Guid ownerId, Guid surveyId)
    {
        var clock = _factory.Services.GetRequiredService<Sgr.Domain.Common.IDateTimeProvider>();
        var hasher = _factory.Services.GetRequiredService<Sgr.Modules.Identity.Authentication.IPasswordHasher>();

        await using (var scope = _factory.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<SgrDbContext>();
            var areaId = db.Users.Single(u => u.Id == ownerId).AreaId!.Value;

            // Si ya existe el collab2 por seed previo en otro test → reutilizamos.
            var existing = db.Users.FirstOrDefault(u => u.Email == "collab2@vialidad.local");
            if (existing is null)
            {
                var u = User.RegisterPending(
                    Guid.NewGuid(),
                    "collab2@vialidad.local",
                    hasher.Hash("Collab2Pwd!"),
                    "Colaborador 2",
                    UserRole.Relevador,
                    areaId,
                    clock.UtcNow);
                u.Accept(clock.UtcNow);
                db.Users.Add(u);
                await db.SaveChangesAsync();
                existing = u;
            }
        }

        var client = _factory.CreateClient();
        var token = await TestAuthHelper.LoginAsync(client, "collab2@vialidad.local", "Collab2Pwd!", "Mobile");
        client.UseBearer(token);

        await using (var scope = _factory.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<SgrDbContext>();
            var userId = db.Users.Single(u => u.Email == "collab2@vialidad.local").Id;
            return (client, userId);
        }
    }

    private async Task SeedPointAsync(Guid pointId, Guid surveyId, Guid createdBy)
    {
        await using var scope = _factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<SgrDbContext>();
        var p = Point.Create(
            id: pointId,
            surveyId: surveyId,
            latitude: 0m, longitude: 0m, accuracyM: null,
            createdBy: createdBy,
            origin: AuditOrigin.MobileCapture,
            captureMode: CaptureModes.Detenido,
            deviceId: "test",
            createdAt: DateTime.UtcNow);
        db.Points.Add(p);
        await db.SaveChangesAsync();
    }

    private static SyncEventDto NewEvent(
        Guid entityId, string eventType, string? field, string? valueJson, Guid authorId, DateTime ts) =>
        new(
            EventId: Guid.NewGuid(),
            EntityType: AuditEntityType.Point,
            EntityId: entityId,
            EventType: eventType,
            Field: field,
            OldValueJson: null,
            NewValueJson: valueJson,
            AuthorId: authorId,
            Origin: AuditOrigin.MobileEdit,
            DeviceId: "test",
            TimestampOriginal: ts);

    private static SyncEventDto NewFieldUpdated(
        Guid pointId, string field, string valueJson, Guid authorId, DateTime ts) =>
        NewEvent(pointId, AuditEventType.FieldUpdated, field, valueJson, authorId, ts);

    private static async Task<SyncPushResponse> PushAsync(HttpClient client, IEnumerable<SyncEventDto> events)
    {
        var resp = await client.PostAsJsonAsync("/api/v1/sync/push", new { events });
        resp.EnsureSuccessStatusCode();
        return (await resp.Content.ReadFromJsonAsync<SyncPushResponse>(JsonOptions))!;
    }
}
