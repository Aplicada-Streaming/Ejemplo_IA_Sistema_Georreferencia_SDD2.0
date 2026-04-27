using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Sgr.Tests.Integration.Common;

namespace Sgr.Tests.Integration.Reports;

/// <summary>
/// Integration tests para los endpoints de reportes (E.6.a):
/// - GET /api/v1/reports/summary
/// - GET /api/v1/surveys/{id}/export?format=csv|xlsx|geojson|zip
/// </summary>
[Collection(WebApiCollection.Name)]
public class ReportsAndExportTests : IClassFixture<SgrApiFactory>
{
    private readonly SgrApiFactory _factory;

    public ReportsAndExportTests(SgrApiFactory factory) => _factory = factory;

    [Fact(DisplayName = "GET /reports/summary devuelve KPIs (admin ve todo)")]
    public async Task Summary_admin_returns_global_KPIs()
    {
        var client = _factory.CreateClient();
        var token = await TestAuthHelper.LoginAsync(client, "admin@vialidad.local", "Admin1234!", "Web");
        client.UseBearer(token);

        var response = await client.GetAsync("/api/v1/reports/summary");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var dto = await response.Content.ReadFromJsonAsync<ReportSummaryDto>();
        dto.Should().NotBeNull();
        dto!.TotalSurveys.Should().BeGreaterThanOrEqualTo(0);
        dto.TotalPoints.Should().BeGreaterThanOrEqualTo(0);
        dto.TotalPhotos.Should().BeGreaterThanOrEqualTo(0);
        dto.Recent.Should().NotBeNull();
    }

    [Fact(DisplayName = "GET /reports/summary requiere autenticación")]
    public async Task Summary_unauthenticated_returns_401()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/api/v1/reports/summary");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact(DisplayName = "Export CSV — devuelve text/csv con BOM UTF-8 y separador ;")]
    public async Task Export_csv_has_correct_format()
    {
        var client = _factory.CreateClient();
        var token = await TestAuthHelper.LoginAsync(client, "relevador@vialidad.local", "Relev1234!", "Web");
        client.UseBearer(token);

        // Creamos un relevamiento para tener algo que exportar
        var surveyId = Guid.NewGuid();
        var createResponse = await client.PostAsJsonAsync("/api/v1/surveys", new
        {
            id = surveyId,
            name = "Export CSV Test",
            origin = "web_edit",
        });
        createResponse.EnsureSuccessStatusCode();

        var response = await client.GetAsync($"/api/v1/surveys/{surveyId}/export?format=csv");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Content.Headers.ContentType?.MediaType.Should().Be("text/csv");
        var bytes = await response.Content.ReadAsByteArrayAsync();
        bytes.Take(3).Should().Equal(new byte[] { 0xEF, 0xBB, 0xBF });   // BOM UTF-8
        var text = System.Text.Encoding.UTF8.GetString(bytes, 3, bytes.Length - 3);
        text.Should().Contain(";");                              // Separador `;`
        text.Should().Contain("id;latitude;longitude");
    }

    [Fact(DisplayName = "Export XLSX — devuelve openxml con magic bytes PK")]
    public async Task Export_xlsx_returns_valid_workbook()
    {
        var client = _factory.CreateClient();
        var token = await TestAuthHelper.LoginAsync(client, "relevador@vialidad.local", "Relev1234!", "Web");
        client.UseBearer(token);

        var surveyId = Guid.NewGuid();
        await client.PostAsJsonAsync("/api/v1/surveys", new
        {
            id = surveyId,
            name = "Export XLSX Test",
            origin = "web_edit",
        });

        var response = await client.GetAsync($"/api/v1/surveys/{surveyId}/export?format=xlsx");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Content.Headers.ContentType?.MediaType
            .Should().Be("application/vnd.openxmlformats-officedocument.spreadsheetml.sheet");
        var bytes = await response.Content.ReadAsByteArrayAsync();
        bytes[0].Should().Be(0x50);   // 'P'
        bytes[1].Should().Be(0x4B);   // 'K'  (ZIP signature)
    }

    [Fact(DisplayName = "Export GeoJSON — FeatureCollection válido")]
    public async Task Export_geojson_is_FeatureCollection()
    {
        var client = _factory.CreateClient();
        var token = await TestAuthHelper.LoginAsync(client, "relevador@vialidad.local", "Relev1234!", "Web");
        client.UseBearer(token);

        var surveyId = Guid.NewGuid();
        await client.PostAsJsonAsync("/api/v1/surveys", new
        {
            id = surveyId,
            name = "Export GeoJSON Test",
            origin = "web_edit",
        });

        var response = await client.GetAsync($"/api/v1/surveys/{surveyId}/export?format=geojson");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Content.Headers.ContentType?.MediaType.Should().Be("application/geo+json");
        var bytes = await response.Content.ReadAsByteArrayAsync();
        using var doc = System.Text.Json.JsonDocument.Parse(bytes);
        doc.RootElement.GetProperty("type").GetString().Should().Be("FeatureCollection");
        doc.RootElement.GetProperty("features").EnumerateArray().Should().BeEmpty();   // sin puntos aún
    }

    [Fact(DisplayName = "Export con format desconocido → 400")]
    public async Task Export_unknown_format_returns_400()
    {
        var client = _factory.CreateClient();
        var token = await TestAuthHelper.LoginAsync(client, "relevador@vialidad.local", "Relev1234!", "Web");
        client.UseBearer(token);

        var surveyId = Guid.NewGuid();
        await client.PostAsJsonAsync("/api/v1/surveys", new
        {
            id = surveyId,
            name = "Bad Format Test",
            origin = "web_edit",
        });

        var response = await client.GetAsync($"/api/v1/surveys/{surveyId}/export?format=pdf");

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    private sealed record ReportSummaryDto(
        int TotalSurveys,
        int OpenSurveys,
        int ClosedSurveys,
        int TotalPoints,
        int TotalPhotos,
        IReadOnlyList<RecentSurveyDto> Recent);

    private sealed record RecentSurveyDto(Guid Id, string Name, string Status, DateTime UpdatedAt);
}
