using System.Net.Http.Headers;
using System.Net.Http.Json;

namespace Sgr.Tests.Integration.Common;

internal static class TestAuthHelper
{
    public static async Task<string> LoginAsync(
        HttpClient client,
        string email,
        string password,
        string clientFront = "Web")
    {
        var response = await client.PostAsJsonAsync("/api/v1/auth/login", new
        {
            email,
            password,
            client = clientFront,
        });
        response.EnsureSuccessStatusCode();
        var body = await response.Content.ReadFromJsonAsync<LoginDto>()
            ?? throw new InvalidOperationException("Login response was null.");
        return body.AccessToken;
    }

    public static void UseBearer(this HttpClient client, string token) =>
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

    public sealed record LoginDto(
        string AccessToken,
        DateTime ExpiresAtUtc,
        Guid UserId,
        string Role,
        Guid? AreaId);
}
