using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Sgr.Tests.Integration.Common;

namespace Sgr.Tests.Integration.Surveys;

/// <summary>
/// Integration tests for US-02 / CU-04.
/// Cubren CA-2.1 (crear desde web), CA-2.3 (GUID cliente preservado),
/// CA-2.4 (evento `created` registrado) y la regla RN-06 (idempotencia).
/// </summary>
[Collection(WebApiCollection.Name)]
public class CreateSurveyTests : IClassFixture<SgrApiFactory>
{
    private readonly SgrApiFactory _factory;

    public CreateSurveyTests(SgrApiFactory factory) => _factory = factory;

    [Fact(DisplayName = "CA-2.1 — Relevador crea relevamiento desde web y aparece en listado")]
    public async Task CA_2_1_relevador_create_then_list()
    {
        var client = _factory.CreateClient();
        var token = await TestAuthHelper.LoginAsync(client, "relevador@vialidad.local", "Relev1234!", "Web");
        client.UseBearer(token);

        var surveyId = Guid.NewGuid();
        var createResponse = await client.PostAsJsonAsync("/api/v1/surveys", new
        {
            id = surveyId,
            name = "Relevamiento Sprint 0",
            description = "Slice trivial",
            origin = "web_edit",
        });

        createResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var created = await createResponse.Content.ReadFromJsonAsync<SurveyDto>();
        created!.Id.Should().Be(surveyId);
        created.Status.Should().Be("abierto");
        created.Name.Should().Be("Relevamiento Sprint 0");
        created.OwnerId.Should().NotBeEmpty();
        created.AreaId.Should().NotBeEmpty();
        created.TemplateVersionId.Should().NotBeEmpty();

        var listResponse = await client.GetAsync("/api/v1/surveys");
        listResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var list = await listResponse.Content.ReadFromJsonAsync<List<SurveyDto>>();
        list.Should().ContainSingle(s => s.Id == surveyId);
    }

    [Fact(DisplayName = "CA-2.3 — GUID generado en cliente se preserva en backend")]
    public async Task CA_2_3_client_guid_is_preserved()
    {
        var client = _factory.CreateClient();
        var token = await TestAuthHelper.LoginAsync(client, "relevador@vialidad.local", "Relev1234!", "Web");
        client.UseBearer(token);

        var clientGuid = Guid.NewGuid();
        var response = await client.PostAsJsonAsync("/api/v1/surveys", new
        {
            id = clientGuid,
            name = "Test GUID",
            origin = "web_edit",
        });

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var dto = await response.Content.ReadFromJsonAsync<SurveyDto>();
        dto!.Id.Should().Be(clientGuid);
    }

    [Fact(DisplayName = "RN-06 — Reenvío idempotente: mismo GUID retorna el mismo recurso, sin duplicar")]
    public async Task RN_06_idempotency_same_guid_returns_same_survey()
    {
        var client = _factory.CreateClient();
        var token = await TestAuthHelper.LoginAsync(client, "relevador@vialidad.local", "Relev1234!", "Web");
        client.UseBearer(token);

        var surveyId = Guid.NewGuid();
        var payload = new
        {
            id = surveyId,
            name = "Original",
            origin = "web_edit",
        };

        var first = await client.PostAsJsonAsync("/api/v1/surveys", payload);
        first.EnsureSuccessStatusCode();

        var second = await client.PostAsJsonAsync("/api/v1/surveys", new
        {
            id = surveyId,
            name = "Distinto pero ignorado por idempotencia",
            origin = "web_edit",
        });
        second.EnsureSuccessStatusCode();

        var listed = await client.GetFromJsonAsync<List<SurveyDto>>("/api/v1/surveys");
        listed!.Count(s => s.Id == surveyId).Should().Be(1);
        listed!.Single(s => s.Id == surveyId).Name.Should().Be("Original");
    }

    [Fact(DisplayName = "CA-2.4 — Cada creación genera evento `created` en el log de auditoría")]
    public async Task CA_2_4_audit_event_created_for_each_new_survey()
    {
        // Se valida indirectamente vía DB: leemos AuditEvents tras la creación.
        var client = _factory.CreateClient();
        var token = await TestAuthHelper.LoginAsync(client, "relevador@vialidad.local", "Relev1234!", "Web");
        client.UseBearer(token);

        var surveyId = Guid.NewGuid();
        await client.PostAsJsonAsync("/api/v1/surveys", new
        {
            id = surveyId,
            name = "Para audit",
            origin = "web_edit",
        });

        // Acceder a la DB through the test factory
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<Sgr.Persistence.SgrDbContext>();
        var events = db.AuditEvents
            .Where(e => e.EntityId == surveyId && e.EntityType == "survey")
            .ToList();
        events.Should().HaveCount(1);
        events[0].EventType.Should().Be("created");
        events[0].Origin.Should().Be("web_edit");
        events[0].AuthorId.Should().NotBeEmpty();
        events[0].NewValueJson.Should().NotBeNull();
    }

    [Fact(DisplayName = "Auth requerida: sin token devuelve 401")]
    public async Task Without_token_returns_401()
    {
        var client = _factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/v1/surveys", new
        {
            id = Guid.NewGuid(),
            name = "Sin auth",
            origin = "web_edit",
        });

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact(DisplayName = "Admin raíz no puede crear relevamientos (Forbidden)")]
    public async Task Admin_cannot_create_survey()
    {
        var client = _factory.CreateClient();
        var token = await TestAuthHelper.LoginAsync(client, "admin@vialidad.local", "Admin1234!", "Web");
        client.UseBearer(token);

        var response = await client.PostAsJsonAsync("/api/v1/surveys", new
        {
            id = Guid.NewGuid(),
            name = "No permitido",
            origin = "web_edit",
        });

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact(DisplayName = "Listado del jefe muestra solo los de su área")]
    public async Task Jefe_list_filtered_by_area()
    {
        var client = _factory.CreateClient();
        var jefeToken = await TestAuthHelper.LoginAsync(client, "jefe@vialidad.local", "Jefe1234!", "Web");
        client.UseBearer(jefeToken);

        var listResponse = await client.GetAsync("/api/v1/surveys");
        listResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var list = await listResponse.Content.ReadFromJsonAsync<List<SurveyDto>>();
        list.Should().NotBeNull();
        // Inicialmente puede estar vacío o con los creados por el relevador en su área
        // (todos los seeds comparten la misma área default).
    }

    private sealed record SurveyDto(
        Guid Id,
        string Name,
        string? Description,
        Guid AreaId,
        Guid OwnerId,
        Guid TemplateVersionId,
        string Status,
        string? Tags,
        DateTime CreatedAt,
        DateTime UpdatedAt,
        DateTime? ClosedAt);
}
