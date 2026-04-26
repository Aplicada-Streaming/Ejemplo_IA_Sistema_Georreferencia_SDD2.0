using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Sgr.Tests.Integration.Common;

namespace Sgr.Tests.Integration.Auth;

/// <summary>
/// Integration tests for US-01 / CU-01.
/// Cubre los criterios de aceptación CA-1.1 a CA-1.6 con la DB seedeada en memoria.
/// Seeds esperados (DatabaseSeeder): admin, jefe activo, relevador activo + un área default.
/// </summary>
[Collection(WebApiCollection.Name)]
public class AuthLoginTests : IClassFixture<SgrApiFactory>
{
    private readonly SgrApiFactory _factory;

    public AuthLoginTests(SgrApiFactory factory) => _factory = factory;

    [Fact(DisplayName = "CA-1.1 — Relevador activo logea en móvil y recibe JWT con role=relevador")]
    public async Task CA_1_1_Relevador_logs_in_from_mobile()
    {
        var client = _factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/v1/auth/login", new
        {
            email = "relevador@vialidad.local",
            password = "Relev1234!",
            client = "Mobile",
            deviceId = "test-device-1",
        });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<LoginResponseDto>();
        body.Should().NotBeNull();
        body!.AccessToken.Should().NotBeNullOrWhiteSpace();
        body.Role.Should().Be("relevador");
        body.UserId.Should().NotBeEmpty();
        body.AreaId.Should().NotBeNull();
    }

    [Fact(DisplayName = "CA-1.2 — Jefe activo es bloqueado en móvil con 403")]
    public async Task CA_1_2_Jefe_blocked_from_mobile()
    {
        var client = _factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/v1/auth/login", new
        {
            email = "jefe@vialidad.local",
            password = "Jefe1234!",
            client = "Mobile",
        });

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetailsDto>();
        problem!.ErrorCode.Should().Be("MobileForbiddenForRole");
    }

    [Fact(DisplayName = "CA-1.4 — Credenciales inválidas devuelven 401 con mensaje genérico")]
    public async Task CA_1_4_Invalid_credentials()
    {
        var client = _factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/v1/auth/login", new
        {
            email = "relevador@vialidad.local",
            password = "WrongPassword",
            client = "Mobile",
        });

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetailsDto>();
        problem!.ErrorCode.Should().Be("InvalidCredentials");
        // El mensaje no distingue qué campo falló (CU-01 E1).
        problem.Detail.Should().Contain("incorrectos");
    }

    [Fact(DisplayName = "CA-1.4 — Email inexistente también devuelve 401 con mismo mensaje")]
    public async Task CA_1_4_Unknown_email_same_response()
    {
        var client = _factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/v1/auth/login", new
        {
            email = "noexiste@vialidad.local",
            password = "any",
            client = "Web",
        });

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetailsDto>();
        problem!.ErrorCode.Should().Be("InvalidCredentials");
    }

    [Fact(DisplayName = "Web acepta admin raíz autenticando con sus credenciales")]
    public async Task Admin_can_login_from_web()
    {
        var client = _factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/v1/auth/login", new
        {
            email = "admin@vialidad.local",
            password = "Admin1234!",
            client = "Web",
        });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<LoginResponseDto>();
        body!.Role.Should().Be("admin_raiz");
        body.AreaId.Should().BeNull();
    }

    [Fact(DisplayName = "Validación de payload: email faltante → 400")]
    public async Task Missing_email_returns_400()
    {
        var client = _factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/v1/auth/login", new
        {
            password = "x",
            client = "Web",
        });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact(DisplayName = "Health endpoint retorna 200 OK")]
    public async Task Health_endpoint_works()
    {
        var client = _factory.CreateClient();
        var response = await client.GetAsync("/health");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact(DisplayName = "Correlation ID se devuelve en el header de la respuesta")]
    public async Task Correlation_id_is_returned()
    {
        var client = _factory.CreateClient();
        var response = await client.GetAsync("/health");
        response.Headers.Should().ContainKey("X-Correlation-ID");
    }

    private sealed record LoginResponseDto(
        string AccessToken,
        DateTime ExpiresAtUtc,
        Guid UserId,
        string Role,
        Guid? AreaId);

    private sealed record ProblemDetailsDto(
        string? Title,
        int? Status,
        string? Detail,
        string? ErrorCode);
}
