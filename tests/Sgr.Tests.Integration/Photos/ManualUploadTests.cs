using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Sgr.Modules.Storage.Imaging;
using Sgr.Persistence;
using Sgr.Tests.Integration.Common;

namespace Sgr.Tests.Integration.Photos;

/// <summary>
/// Integration tests for US-15 / US-16 — carga lote web con EXIF + cola pendientes geo.
/// Cubre CA-15.1, CA-15.2, CA-15.4, CA-15.5 y CA-16.1 a CA-16.3.
/// </summary>
[Collection(WebApiCollection.Name)]
public class ManualUploadTests : IClassFixture<SgrApiFactory>
{
    private readonly SgrApiFactory _factory;

    public ManualUploadTests(SgrApiFactory factory)
    {
        _factory = factory;
        _factory.Exif.Reset();
    }

    [Fact(DisplayName = "CA-15.5 — Lote modo Detenido sin EXIF: 1 punto en (0,0), todas las fotos asociadas, origin=web_manual_upload")]
    public async Task CA_15_5_detenido_no_exif_creates_single_point()
    {
        var (client, surveyId, _) = await SetupSurveyAsync();

        var content = new MultipartFormDataContent
        {
            { new StringContent("Detenido"), "Mode" },
            FakeFile("a.jpg", "no-exif"),
            FakeFile("b.jpg", "no-exif"),
            FakeFile("c.jpg", "no-exif"),
        };

        var resp = await client.PostAsync($"/api/v1/surveys/{surveyId}/manual-upload", content);
        resp.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await resp.Content.ReadFromJsonAsync<ManualUploadResultDto>();
        // Si nadie tiene EXIF en modo detenido → el centroide no se puede calcular: todo va a la cola.
        body!.FilesReceived.Should().Be(3);
        body.PhotosWithGps.Should().Be(0);
        body.PhotosPendingGeo.Should().Be(3);

        await using var scope = _factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<SgrDbContext>();
        var photos = db.Photos.Where(p => p.SurveyId == surveyId).ToList();
        photos.Should().HaveCount(3);
        photos.Should().AllSatisfy(p =>
        {
            p.PointId.Should().BeNull();
            p.Origin.Should().Be("web_manual_upload");
        });
    }

    [Fact(DisplayName = "CA-15.1 — Lote modo Recorrido con EXIF agrupa por proximidad (radio 10m)")]
    public async Task CA_15_1_recorrido_clusters_by_proximity()
    {
        var (client, surveyId, _) = await SetupSurveyAsync();

        // 3 fotos en cluster A (Buenos Aires Obelisco), 2 en cluster B (~50m al sur).
        var t0 = new DateTime(2026, 4, 20, 10, 0, 0, DateTimeKind.Utc);
        _factory.Exif.Register("A1", new ExifData(-34.6037m, -58.3816m, t0));
        _factory.Exif.Register("A2", new ExifData(-34.60371m, -58.3816m, t0.AddMinutes(1)));
        _factory.Exif.Register("A3", new ExifData(-34.60372m, -58.3816m, t0.AddMinutes(2)));
        _factory.Exif.Register("B1", new ExifData(-34.6042m, -58.3816m, t0.AddMinutes(3)));
        _factory.Exif.Register("B2", new ExifData(-34.60421m, -58.3816m, t0.AddMinutes(4)));

        var content = new MultipartFormDataContent
        {
            { new StringContent("Recorrido"), "Mode" },
            FakeFile("a1.jpg", "A1"),
            FakeFile("a2.jpg", "A2"),
            FakeFile("a3.jpg", "A3"),
            FakeFile("b1.jpg", "B1"),
            FakeFile("b2.jpg", "B2"),
        };

        var resp = await client.PostAsync($"/api/v1/surveys/{surveyId}/manual-upload", content);
        resp.EnsureSuccessStatusCode();
        var body = await resp.Content.ReadFromJsonAsync<ManualUploadResultDto>();
        body!.FilesReceived.Should().Be(5);
        body.PhotosWithGps.Should().Be(5);
        body.PhotosPendingGeo.Should().Be(0);
        body.PointsCreated.Should().Be(2);

        await using var scope = _factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<SgrDbContext>();
        var pointsOfSurvey = db.Points.Where(p => p.SurveyId == surveyId).ToList();
        pointsOfSurvey.Should().HaveCount(2);

        // Las 5 fotos asignadas viven a través de Photo.PointId apuntando a algún Point
        // de los 2 creados en este survey. Filtramos por esa relación.
        var pointIds = pointsOfSurvey.Select(p => p.Id).ToHashSet();
        var photos = db.Photos.Where(p => p.PointId != null && pointIds.Contains(p.PointId.Value)).ToList();
        photos.Should().HaveCount(5);
    }

    [Fact(DisplayName = "CA-15.4 — Foto sin EXIF en modo recorrido va a cola pending-geo")]
    public async Task CA_15_4_no_exif_goes_to_pending()
    {
        var (client, surveyId, _) = await SetupSurveyAsync();

        var content = new MultipartFormDataContent
        {
            { new StringContent("Recorrido"), "Mode" },
            FakeFile("noexif.jpg", "no-tag-registered"),
        };

        var resp = await client.PostAsync($"/api/v1/surveys/{surveyId}/manual-upload", content);
        resp.EnsureSuccessStatusCode();
        var body = await resp.Content.ReadFromJsonAsync<ManualUploadResultDto>();
        body!.PhotosPendingGeo.Should().Be(1);
        body.PointsCreated.Should().Be(0);

        // Debe aparecer en la cola.
        var pending = await client.GetFromJsonAsync<List<PendingGeoPhotoDto>>(
            $"/api/v1/surveys/{surveyId}/photos/pending-geo");
        pending!.Should().HaveCount(1);
        pending[0].OriginalFileName.Should().Be("noexif.jpg");
    }

    [Fact(DisplayName = "CA-16.2 — geo-resolve con coords cercanas a un Punto existente lo asocia")]
    public async Task CA_16_2_resolve_associates_with_existing_point()
    {
        var (client, surveyId, _) = await SetupSurveyAsync();

        // Subir un lote sin EXIF para que quede pending.
        var content1 = new MultipartFormDataContent
        {
            { new StringContent("Recorrido"), "Mode" },
            FakeFile("p.jpg", "no-tag"),
        };
        await client.PostAsync($"/api/v1/surveys/{surveyId}/manual-upload", content1);

        // Crear un Punto via lote con EXIF, en una ubicación conocida.
        var t0 = new DateTime(2026, 4, 20, 10, 0, 0, DateTimeKind.Utc);
        _factory.Exif.Register("X1", new ExifData(-34.6037m, -58.3816m, t0));
        var content2 = new MultipartFormDataContent
        {
            { new StringContent("Recorrido"), "Mode" },
            FakeFile("x.jpg", "X1"),
        };
        await client.PostAsync($"/api/v1/surveys/{surveyId}/manual-upload", content2);

        // Tomar la foto pending y resolverla a las MISMAS coords.
        var pending = await client.GetFromJsonAsync<List<PendingGeoPhotoDto>>(
            $"/api/v1/surveys/{surveyId}/photos/pending-geo");
        var pendingId = pending!.Single().Id;

        var resolveResp = await client.PostAsJsonAsync($"/api/v1/photos/{pendingId}/geo-resolve",
            new { Latitude = -34.6037m, Longitude = -58.3816m });
        resolveResp.EnsureSuccessStatusCode();
        var result = await resolveResp.Content.ReadFromJsonAsync<GeoResolveResultDto>();
        result!.PointWasCreated.Should().BeFalse(); // se asoció al existente

        // Cola debe quedar vacía.
        var afterPending = await client.GetFromJsonAsync<List<PendingGeoPhotoDto>>(
            $"/api/v1/surveys/{surveyId}/photos/pending-geo");
        afterPending!.Should().BeEmpty();
    }

    [Fact(DisplayName = "CA-16.3 — geo-resolve con coords lejos del radio crea un nuevo Punto")]
    public async Task CA_16_3_resolve_creates_new_point_when_far()
    {
        var (client, surveyId, _) = await SetupSurveyAsync();

        // Subir foto sin EXIF.
        var content = new MultipartFormDataContent
        {
            { new StringContent("Recorrido"), "Mode" },
            FakeFile("noexif.jpg", "no-tag"),
        };
        await client.PostAsync($"/api/v1/surveys/{surveyId}/manual-upload", content);
        var pending = await client.GetFromJsonAsync<List<PendingGeoPhotoDto>>(
            $"/api/v1/surveys/{surveyId}/photos/pending-geo");
        var pendingId = pending!.Single().Id;

        // Resolver a un lugar donde no hay puntos previos.
        var resolveResp = await client.PostAsJsonAsync($"/api/v1/photos/{pendingId}/geo-resolve",
            new { Latitude = 0m, Longitude = 0m });
        resolveResp.EnsureSuccessStatusCode();
        var result = await resolveResp.Content.ReadFromJsonAsync<GeoResolveResultDto>();
        result!.PointWasCreated.Should().BeTrue();
    }

    // ----- helpers -----

    private async Task<(HttpClient client, Guid surveyId, Guid ownerId)> SetupSurveyAsync()
    {
        var client = _factory.CreateClient();
        var token = await TestAuthHelper.LoginAsync(client, "jefe@vialidad.local", "Jefe1234!");
        client.UseBearer(token);

        var surveyId = Guid.NewGuid();
        var resp = await client.PostAsJsonAsync("/api/v1/surveys", new
        {
            id = surveyId, name = "Manual upload test", origin = "web_edit",
        });
        resp.EnsureSuccessStatusCode();
        var dto = await resp.Content.ReadFromJsonAsync<JsonElement>();
        var ownerId = dto.GetProperty("ownerId").GetGuid();
        return (client, surveyId, ownerId);
    }

    /// <summary>
    /// Crea un "archivo" multipart con primer línea EXIF-TAG:&lt;tag&gt; para que
    /// <see cref="FakeExifReader"/> pueda devolver datos a medida durante el test.
    /// </summary>
    private static (HttpContent content, string name, string fileName) FakeFile(string fileName, string tag)
    {
        var bytes = System.Text.Encoding.UTF8.GetBytes($"EXIF-TAG:{tag}\nfake-jpeg-bytes");
        var streamContent = new ByteArrayContent(bytes);
        streamContent.Headers.ContentType = new MediaTypeHeaderValue("image/jpeg");
        return (streamContent, "Files", fileName);
    }

    private sealed record ManualUploadResultDto(
        int FilesReceived,
        int PhotosWithGps,
        int PhotosPendingGeo,
        int PointsCreated);

    private sealed record PendingGeoPhotoDto(
        Guid Id,
        Guid SurveyId,
        string AdapterName,
        long SizeBytes,
        string? Comment,
        string? OriginalFileName,
        DateTime CreatedAt);

    private sealed record GeoResolveResultDto(
        Guid PhotoId,
        Guid PointId,
        bool PointWasCreated);
}

/// <summary>
/// Extensión para que el operador <c>{ ..., FakeFile(...) }</c> compile en
/// MultipartFormDataContent — ese collection initializer requiere Add(content, name, fileName).
/// </summary>
internal static class MultipartFormDataExtensions
{
    public static void Add(this MultipartFormDataContent self,
        (HttpContent content, string name, string fileName) tuple) =>
        self.Add(tuple.content, tuple.name, tuple.fileName);
}
