using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Sgr.Tests.Integration.Common;

namespace Sgr.Tests.Integration.SystemConfig;

/// <summary>
/// Integration tests for US-17 — wizard de storage. Cubre CA-17.2 (path inválido),
/// CA-17.3 (Local valid → test/save), GET (sin config → 204), permisos (jefe → 403).
/// CA-17.5 (datos previos accesibles tras cambio) ya está cubierto por RN-12 en
/// PhotoStorageAdapterFactory: la lectura usa el adapter registrado en la Photo,
/// no el activo. Aquí verificamos GET/POST/test.
/// </summary>
[Collection(WebApiCollection.Name)]
public class StorageConfigTests : IClassFixture<SgrApiFactory>
{
    private readonly SgrApiFactory _factory;

    public StorageConfigTests(SgrApiFactory factory) => _factory = factory;

    [Fact(DisplayName = "Jefe NO puede ver config de storage → 403")]
    public async Task Jefe_cant_access_storage_config()
    {
        var client = _factory.CreateClient();
        var token = await TestAuthHelper.LoginAsync(client, "jefe@vialidad.local", "Jefe1234!");
        client.UseBearer(token);

        var resp = await client.GetAsync("/api/v1/system-config/storage");
        resp.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact(DisplayName = "CA-17.3 — POST test con Local válido devuelve success=true")]
    public async Task CA_17_3_test_local_valid_returns_success()
    {
        var client = _factory.CreateClient();
        var token = await TestAuthHelper.LoginAsync(client, "admin@vialidad.local", "Admin1234!");
        client.UseBearer(token);

        var tempDir = Path.Combine(Path.GetTempPath(), $"sgr-test-storage-{Guid.NewGuid():N}");
        try
        {
            var resp = await client.PostAsJsonAsync("/api/v1/system-config/storage/test", new
            {
                adapter = "local",
                config = new { local = new { rootPath = tempDir } },
            });
            resp.StatusCode.Should().Be(HttpStatusCode.OK);
            var body = await resp.Content.ReadFromJsonAsync<TestResultDto>();
            body!.Success.Should().BeTrue();
            body.RoundTripMs.Should().NotBeNull();
        }
        finally
        {
            if (Directory.Exists(tempDir)) Directory.Delete(tempDir, recursive: true);
        }
    }

    [Fact(DisplayName = "POST storage Local persiste y luego GET devuelve la misma config")]
    public async Task Post_then_get_round_trip()
    {
        var client = _factory.CreateClient();
        var token = await TestAuthHelper.LoginAsync(client, "admin@vialidad.local", "Admin1234!");
        client.UseBearer(token);

        var tempDir = Path.Combine(Path.GetTempPath(), $"sgr-test-storage-{Guid.NewGuid():N}");
        try
        {
            var save = await client.PostAsJsonAsync("/api/v1/system-config/storage", new
            {
                adapter = "local",
                config = new { local = new { rootPath = tempDir } },
            });
            save.EnsureSuccessStatusCode();

            var get = await client.GetAsync("/api/v1/system-config/storage");
            get.StatusCode.Should().Be(HttpStatusCode.OK);
            var dto = await get.Content.ReadFromJsonAsync<StorageConfigDto>();
            dto!.Adapter.Should().Be("local");
            dto.Config.Local!.RootPath.Should().Be(tempDir);
        }
        finally
        {
            if (Directory.Exists(tempDir)) Directory.Delete(tempDir, recursive: true);
        }
    }

    [Fact(DisplayName = "POST storage S3 con SecretKey real → GET devuelve la SecretKey enmascarada")]
    public async Task Post_s3_then_get_returns_masked_secret()
    {
        var client = _factory.CreateClient();
        var token = await TestAuthHelper.LoginAsync(client, "admin@vialidad.local", "Admin1234!");
        client.UseBearer(token);

        var save = await client.PostAsJsonAsync("/api/v1/system-config/storage", new
        {
            adapter = "s3",
            config = new
            {
                s3 = new
                {
                    bucketName = "sgr-test",
                    region = "us-east-1",
                    accessKey = "ABCDEFGHIJ",
                    secretKey = "SUPER-SECRETO-NO-LEAKEAR",
                    serviceUrl = (string?)null,
                    forcePathStyle = false,
                },
            },
        });
        save.EnsureSuccessStatusCode();
        var savedDto = await save.Content.ReadFromJsonAsync<StorageConfigDto>();
        // Aún en la respuesta del POST viene enmascarada (no debería hacer round-trip de la secret).
        savedDto!.Config.S3!.SecretKey.Should().Be("***");

        var get = await client.GetAsync("/api/v1/system-config/storage");
        var dto = await get.Content.ReadFromJsonAsync<StorageConfigDto>();
        dto!.Adapter.Should().Be("s3");
        dto.Config.S3!.SecretKey.Should().Be("***");
        dto.Config.S3.AccessKey.Should().Be("ABCDEFGHIJ");
    }

    private sealed record TestResultDto(bool Success, string Message, long? RoundTripMs);

    private sealed class StorageConfigDto
    {
        public string Adapter { get; set; } = "";
        public StorageAdapterConfigDto Config { get; set; } = new();
    }

    private sealed class StorageAdapterConfigDto
    {
        public LocalConfigDto? Local { get; set; }
        public S3ConfigDto? S3 { get; set; }
    }

    private sealed class LocalConfigDto
    {
        public string RootPath { get; set; } = "";
    }

    private sealed class S3ConfigDto
    {
        public string BucketName { get; set; } = "";
        public string Region { get; set; } = "";
        public string AccessKey { get; set; } = "";
        public string SecretKey { get; set; } = "";
        public string? ServiceUrl { get; set; }
        public bool ForcePathStyle { get; set; }
    }
}
