using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Sgr.Domain.Identity;
using Sgr.Persistence;
using Sgr.Tests.Integration.Common;

namespace Sgr.Tests.Integration.Auth;

/// <summary>
/// Integration tests for US-13 — Aceptación jerárquica admin → jefe → relevador.
/// Cubre CA-13.1 a CA-13.5 con la DB seedeada en memoria.
/// </summary>
[Collection(WebApiCollection.Name)]
public class UserAcceptanceTests : IClassFixture<SgrApiFactory>
{
    private readonly SgrApiFactory _factory;

    public UserAcceptanceTests(SgrApiFactory factory) => _factory = factory;

    [Fact(DisplayName = "GET /api/v1/areas devuelve al menos el área seed (anónimo)")]
    public async Task Areas_listing_is_anonymous_and_returns_seed()
    {
        var client = _factory.CreateClient();
        var areas = await client.GetFromJsonAsync<List<AreaDto>>("/api/v1/areas");
        areas.Should().NotBeNull();
        areas!.Should().NotBeEmpty();
    }

    [Fact(DisplayName = "CA-13.1 — Jefe se auto-registra → admin acepta → jefe puede loguear")]
    public async Task CA_13_1_Jefe_register_then_admin_accepts()
    {
        var client = _factory.CreateClient();
        var areaId = await GetSeedAreaIdAsync(client);

        // 1. Jefe se registra (anónimo).
        var registered = await Register(client, "nuevo-jefe@vialidad.local", "JefePwd1234!", "Nuevo Jefe", "jefe_area", areaId);
        registered.StatusCode.Should().Be(HttpStatusCode.Created);

        // 2. No puede loguear todavía: 403 PendingAcceptance.
        var loginPending = await client.PostAsJsonAsync("/api/v1/auth/login", new
        {
            email = "nuevo-jefe@vialidad.local", password = "JefePwd1234!", client = "Web",
        });
        loginPending.StatusCode.Should().Be(HttpStatusCode.Forbidden);

        // 3. Admin acepta.
        var adminToken = await TestAuthHelper.LoginAsync(client, "admin@vialidad.local", "Admin1234!");
        client.UseBearer(adminToken);

        var pending = await client.GetFromJsonAsync<List<UserDto>>("/api/v1/users/pending");
        pending.Should().NotBeNull();
        var jefeToAccept = pending!.Single(u => u.Email == "nuevo-jefe@vialidad.local");

        var accept = await client.PostAsync($"/api/v1/users/{jefeToAccept.Id}/accept", null);
        accept.StatusCode.Should().Be(HttpStatusCode.OK);

        // 4. Ahora sí puede loguear.
        client.DefaultRequestHeaders.Authorization = null;
        var loginAccepted = await client.PostAsJsonAsync("/api/v1/auth/login", new
        {
            email = "nuevo-jefe@vialidad.local", password = "JefePwd1234!", client = "Web",
        });
        loginAccepted.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact(DisplayName = "CA-13.2 — Relevador se registra → jefe del área acepta → puede loguear")]
    public async Task CA_13_2_Jefe_accepts_relevador_of_own_area()
    {
        var client = _factory.CreateClient();
        var areaId = await GetSeedAreaIdAsync(client);

        // Relevador se registra.
        await Register(client, "nuevo-relev@vialidad.local", "RelevPwd1234!", "Nuevo Relevador", "relevador", areaId);

        // Jefe del seed acepta (mismo area).
        var jefeToken = await TestAuthHelper.LoginAsync(client, "jefe@vialidad.local", "Jefe1234!");
        client.UseBearer(jefeToken);

        var pending = await client.GetFromJsonAsync<List<UserDto>>("/api/v1/users/pending");
        var relev = pending!.Single(u => u.Email == "nuevo-relev@vialidad.local");
        var accept = await client.PostAsync($"/api/v1/users/{relev.Id}/accept", null);
        accept.StatusCode.Should().Be(HttpStatusCode.OK);

        client.DefaultRequestHeaders.Authorization = null;
        var login = await client.PostAsJsonAsync("/api/v1/auth/login", new
        {
            email = "nuevo-relev@vialidad.local", password = "RelevPwd1234!", client = "Mobile", deviceId = "test",
        });
        login.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact(DisplayName = "CA-13.3 — Admin rechaza jefe → estado dado_de_baja, no puede loguear")]
    public async Task CA_13_3_Admin_rejects_jefe()
    {
        var client = _factory.CreateClient();
        var areaId = await GetSeedAreaIdAsync(client);

        await Register(client, "rechazado@vialidad.local", "RechPwd1234!", "Rechazado", "jefe_area", areaId);

        var adminToken = await TestAuthHelper.LoginAsync(client, "admin@vialidad.local", "Admin1234!");
        client.UseBearer(adminToken);

        var pending = await client.GetFromJsonAsync<List<UserDto>>("/api/v1/users/pending");
        var u = pending!.Single(x => x.Email == "rechazado@vialidad.local");
        var reject = await client.PostAsync($"/api/v1/users/{u.Id}/reject", null);
        reject.StatusCode.Should().Be(HttpStatusCode.OK);

        client.DefaultRequestHeaders.Authorization = null;
        var login = await client.PostAsJsonAsync("/api/v1/auth/login", new
        {
            email = "rechazado@vialidad.local", password = "RechPwd1234!", client = "Web",
        });
        login.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact(DisplayName = "CA-13.4 — Admin inhabilita jefe activo y luego puede rehabilitarlo")]
    public async Task CA_13_4_Admin_disable_then_enable_jefe()
    {
        var client = _factory.CreateClient();
        var areaId = await GetSeedAreaIdAsync(client);

        await Register(client, "tobedisabled@vialidad.local", "DisPwd1234!", "ToDisable", "jefe_area", areaId);

        var adminToken = await TestAuthHelper.LoginAsync(client, "admin@vialidad.local", "Admin1234!");
        client.UseBearer(adminToken);

        var pending = await client.GetFromJsonAsync<List<UserDto>>("/api/v1/users/pending");
        var jefe = pending!.Single(u => u.Email == "tobedisabled@vialidad.local");
        await client.PostAsync($"/api/v1/users/{jefe.Id}/accept", null);

        var disable = await client.PostAsync($"/api/v1/users/{jefe.Id}/disable", null);
        disable.StatusCode.Should().Be(HttpStatusCode.OK);

        // No puede loguear.
        client.DefaultRequestHeaders.Authorization = null;
        var loginDisabled = await client.PostAsJsonAsync("/api/v1/auth/login", new
        {
            email = "tobedisabled@vialidad.local", password = "DisPwd1234!", client = "Web",
        });
        loginDisabled.StatusCode.Should().Be(HttpStatusCode.Forbidden);

        // Admin lo rehabilita.
        client.UseBearer(adminToken);
        var enable = await client.PostAsync($"/api/v1/users/{jefe.Id}/enable", null);
        enable.StatusCode.Should().Be(HttpStatusCode.OK);

        client.DefaultRequestHeaders.Authorization = null;
        var loginAfter = await client.PostAsJsonAsync("/api/v1/auth/login", new
        {
            email = "tobedisabled@vialidad.local", password = "DisPwd1234!", client = "Web",
        });
        loginAfter.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact(DisplayName = "CA-13.5 — Jefe intenta aceptar relevador de OTRA área → 403")]
    public async Task CA_13_5_Jefe_cant_accept_relevador_of_other_area()
    {
        var client = _factory.CreateClient();

        // Crear una segunda área directamente vía DbContext (no hay endpoint de creación).
        var otherAreaId = await CreateExtraAreaAsync("Área Otra");
        var seedAreaId = await GetSeedAreaIdAsync(client);
        otherAreaId.Should().NotBe(seedAreaId);

        // Relevador se registra en la OTRA área.
        await Register(client, "relev-otra@vialidad.local", "RelevPwd1234!", "Otra Area Relev", "relevador", otherAreaId);

        // Jefe del seed (con seedAreaId) intenta listar pendientes — no debería ver al relevador de otra área.
        var jefeToken = await TestAuthHelper.LoginAsync(client, "jefe@vialidad.local", "Jefe1234!");
        client.UseBearer(jefeToken);

        var pending = await client.GetFromJsonAsync<List<UserDto>>("/api/v1/users/pending");
        pending.Should().NotContain(u => u.Email == "relev-otra@vialidad.local");

        // Y si intenta aceptar por id directo → 403 Forbidden.
        // Para conseguir el id del relevador uso un admin (que ve todos) y luego pruebo desde el jefe.
        client.DefaultRequestHeaders.Authorization = null;
        var adminToken = await TestAuthHelper.LoginAsync(client, "admin@vialidad.local", "Admin1234!");
        client.UseBearer(adminToken);
        var allFromAdmin = await client.GetFromJsonAsync<List<UserDto>>("/api/v1/users/pending");
        // admin sólo ve jefes pendientes, no relevadores; usamos el listado completo.
        var allUsers = await client.GetFromJsonAsync<List<UserDto>>("/api/v1/users");
        // admin sólo lista jefes, así que tampoco ve al relevador. Voy a leer directo del DbContext.
        var relevId = await GetUserIdByEmailAsync("relev-otra@vialidad.local");

        client.DefaultRequestHeaders.Authorization = null;
        client.UseBearer(jefeToken);
        var attempt = await client.PostAsync($"/api/v1/users/{relevId}/accept", null);
        attempt.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    private async Task<HttpResponseMessage> Register(
        HttpClient client, string email, string password, string fullName, string role, Guid? areaId)
    {
        return await client.PostAsJsonAsync("/api/v1/auth/register", new
        {
            email, password, fullName, role, areaId,
        });
    }

    private async Task<Guid> GetSeedAreaIdAsync(HttpClient client)
    {
        var areas = await client.GetFromJsonAsync<List<AreaDto>>("/api/v1/areas");
        return areas!.First(a => a.Name == "Área Default").Id;
    }

    private async Task<Guid> CreateExtraAreaAsync(string name)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<SgrDbContext>();
        var area = new Area(Guid.NewGuid(), name, "extra", DateTime.UtcNow);
        db.Areas.Add(area);
        await db.SaveChangesAsync();
        return area.Id;
    }

    private async Task<Guid> GetUserIdByEmailAsync(string email)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<SgrDbContext>();
        var u = db.Users.Single(x => x.Email == email);
        await Task.CompletedTask;
        return u.Id;
    }

    private sealed record AreaDto(Guid Id, string Name, string? Description);

    private sealed record UserDto(
        Guid Id,
        string Email,
        string FullName,
        string Role,
        string Status,
        Guid? AreaId,
        DateTime? AcceptedAt,
        DateTime CreatedAt);
}
