using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Sgr.Tests.Integration.Common;

namespace Sgr.Tests.Integration.Templates;

/// <summary>
/// Integration tests para el editor de plantillas (E.5.c):
///   - POST   /api/v1/templates                    — crear (sólo admin)
///   - PUT    /api/v1/templates/versions/{id}/...  — editar borrador
///   - POST   /api/v1/templates/versions/{id}/publish
/// </summary>
[Collection(WebApiCollection.Name)]
public class TemplateEditorTests : IClassFixture<SgrApiFactory>
{
    private readonly SgrApiFactory _factory;

    public TemplateEditorTests(SgrApiFactory factory) => _factory = factory;

    [Fact(DisplayName = "Admin crea plantilla → recibe templateId + draftVersionId")]
    public async Task Create_template_as_admin()
    {
        var client = _factory.CreateClient();
        var token = await TestAuthHelper.LoginAsync(client, "admin@vialidad.local", "Admin1234!", "Web");
        client.UseBearer(token);

        var response = await client.PostAsJsonAsync("/api/v1/templates", new { name = "Plantilla puentes" });

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var dto = await response.Content.ReadFromJsonAsync<TemplateCreatedDto>();
        dto!.TemplateId.Should().NotBeEmpty();
        dto.DraftVersionId.Should().NotBeEmpty();
    }

    [Fact(DisplayName = "Jefe NO puede crear plantilla → 403")]
    public async Task Create_template_as_jefe_returns_403()
    {
        var client = _factory.CreateClient();
        var token = await TestAuthHelper.LoginAsync(client, "jefe@vialidad.local", "Jefe1234!", "Web");
        client.UseBearer(token);

        var response = await client.PostAsJsonAsync("/api/v1/templates", new { name = "X" });

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact(DisplayName = "Update fields + publicar transiciona a publicada")]
    public async Task Update_fields_then_publish()
    {
        var client = _factory.CreateClient();
        var token = await TestAuthHelper.LoginAsync(client, "admin@vialidad.local", "Admin1234!", "Web");
        client.UseBearer(token);

        var created = await (await client.PostAsJsonAsync("/api/v1/templates",
                new { name = "Test publicar" }))
            .Content.ReadFromJsonAsync<TemplateCreatedDto>();

        var newFields = "[{\"key\":\"k\",\"label\":\"K\",\"type\":\"texto\",\"required\":true}]";
        var putFieldsResponse = await client.PutAsJsonAsync(
            $"/api/v1/templates/versions/{created!.DraftVersionId}/fields",
            new { fieldDefinitionsJson = newFields });
        putFieldsResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var publishResponse = await client.PostAsync(
            $"/api/v1/templates/versions/{created.DraftVersionId}/publish", null);
        publishResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

        // GET la versión y verificar
        var get = await client.GetAsync($"/api/v1/templates/versions/{created.DraftVersionId}");
        get.StatusCode.Should().Be(HttpStatusCode.OK);
        var detail = await get.Content.ReadFromJsonAsync<TemplateVersionDetailDto>();
        detail!.Status.Should().Be("publicada");
        detail.PublishedAt.Should().NotBeNull();
        detail.Fields.Should().HaveCount(1);
        detail.Fields[0].Key.Should().Be("k");
    }

    [Fact(DisplayName = "Update fields en versión publicada → 409")]
    public async Task Update_fields_on_publicada_returns_409()
    {
        var client = _factory.CreateClient();
        var token = await TestAuthHelper.LoginAsync(client, "admin@vialidad.local", "Admin1234!", "Web");
        client.UseBearer(token);

        var created = await (await client.PostAsJsonAsync("/api/v1/templates", new { name = "T2" }))
            .Content.ReadFromJsonAsync<TemplateCreatedDto>();

        // Publicar
        await client.PostAsync($"/api/v1/templates/versions/{created!.DraftVersionId}/publish", null);

        // Intentar update — debería rechazar
        var response = await client.PutAsJsonAsync(
            $"/api/v1/templates/versions/{created.DraftVersionId}/fields",
            new { fieldDefinitionsJson = "[]" });

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact(DisplayName = "Publish dos veces es idempotente (segundo retorna 204)")]
    public async Task Publish_idempotent()
    {
        var client = _factory.CreateClient();
        var token = await TestAuthHelper.LoginAsync(client, "admin@vialidad.local", "Admin1234!", "Web");
        client.UseBearer(token);

        var created = await (await client.PostAsJsonAsync("/api/v1/templates", new { name = "T3" }))
            .Content.ReadFromJsonAsync<TemplateCreatedDto>();

        var first = await client.PostAsync($"/api/v1/templates/versions/{created!.DraftVersionId}/publish", null);
        var second = await client.PostAsync($"/api/v1/templates/versions/{created.DraftVersionId}/publish", null);

        first.StatusCode.Should().Be(HttpStatusCode.NoContent);
        second.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    private sealed record TemplateCreatedDto(Guid TemplateId, Guid DraftVersionId);

    private sealed record TemplateVersionDetailDto(
        Guid VersionId,
        Guid TemplateId,
        string TemplateName,
        int VersionNumber,
        string Status,
        DateTime? PublishedAt,
        IReadOnlyList<FieldDefinitionDto> Fields,
        System.Text.Json.JsonElement CaptureParams);

    private sealed record FieldDefinitionDto(
        string Key,
        string Label,
        string Type,
        bool Required,
        IReadOnlyList<string>? Options);
}
