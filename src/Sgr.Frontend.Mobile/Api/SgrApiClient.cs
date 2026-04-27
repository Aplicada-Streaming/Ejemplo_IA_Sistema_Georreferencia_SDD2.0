using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Sgr.Frontend.Mobile.Auth;

namespace Sgr.Frontend.Mobile.Api;

public interface ISgrApiClient
{
    Task<LoginResult> LoginAsync(string email, string password, CancellationToken ct = default);
    Task<IReadOnlyList<SurveyDto>> ListSurveysAsync(CancellationToken ct = default);
    Task<SurveyDto> CreateSurveyAsync(CreateSurveyDto request, CancellationToken ct = default);
}

public sealed class SgrApiClient : ISgrApiClient
{
    private readonly HttpClient _http;
    private readonly IMobileTokenStore _store;
    private readonly IDeviceIdProvider _device;

    public SgrApiClient(HttpClient http, IMobileTokenStore store, IDeviceIdProvider device)
    {
        _http = http;
        _store = store;
        _device = device;
    }

    public async Task<LoginResult> LoginAsync(string email, string password, CancellationToken ct = default)
    {
        var response = await _http.PostAsJsonAsync("/api/v1/auth/login", new
        {
            email,
            password,
            client = "Mobile",
            deviceId = _device.GetDeviceId(),
        }, ct);

        if (response.StatusCode == HttpStatusCode.Unauthorized)
            return new LoginResult.InvalidCredentials();
        if (response.StatusCode == HttpStatusCode.Forbidden)
        {
            var problem = await ReadProblemAsync(response, ct);
            return new LoginResult.Forbidden(problem?.Detail ?? "Acceso no permitido en móvil.");
        }

        if (!response.IsSuccessStatusCode)
        {
            var problem = await ReadProblemAsync(response, ct);
            return new LoginResult.Error(problem?.Detail ?? $"Error inesperado ({(int)response.StatusCode}).");
        }

        var body = await response.Content.ReadFromJsonAsync<LoginResponseDto>(cancellationToken: ct)
            ?? throw new InvalidOperationException("Respuesta de login vacía.");
        return new LoginResult.Ok(body);
    }

    public async Task<IReadOnlyList<SurveyDto>> ListSurveysAsync(CancellationToken ct = default)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/v1/surveys");
        await AttachAuthAsync(request);

        using var response = await _http.SendAsync(request, ct);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<IReadOnlyList<SurveyDto>>(cancellationToken: ct)
            ?? Array.Empty<SurveyDto>();
    }

    public async Task<SurveyDto> CreateSurveyAsync(CreateSurveyDto requestDto, CancellationToken ct = default)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/v1/surveys")
        {
            Content = JsonContent.Create(requestDto),
        };
        await AttachAuthAsync(request);

        using var response = await _http.SendAsync(request, ct);
        if (!response.IsSuccessStatusCode)
        {
            var problem = await ReadProblemAsync(response, ct);
            throw new SgrApiException(
                (int)response.StatusCode,
                problem?.Title ?? response.ReasonPhrase ?? "Error",
                problem?.Detail ?? "El servidor respondió con un error.",
                problem?.ErrorCode);
        }

        return await response.Content.ReadFromJsonAsync<SurveyDto>(cancellationToken: ct)
            ?? throw new InvalidOperationException("Respuesta vacía.");
    }

    private async Task AttachAuthAsync(HttpRequestMessage request)
    {
        var session = await _store.GetAsync();
        if (session is not null)
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", session.AccessToken);
    }

    private static async Task<ProblemDetailsDto?> ReadProblemAsync(HttpResponseMessage response, CancellationToken ct)
    {
        try
        {
            return await response.Content.ReadFromJsonAsync<ProblemDetailsDto>(
                new JsonSerializerOptions(JsonSerializerDefaults.Web), ct);
        }
        catch { return null; }
    }
}

public interface IDeviceIdProvider
{
    string GetDeviceId();
}

public sealed class MauiDeviceIdProvider : IDeviceIdProvider
{
    private string? _cachedId;

    public string GetDeviceId()
    {
        if (_cachedId is not null) return _cachedId;

        // Stable per-installation id stored in Preferences. Not as strong as Android's
        // AdvertisingId / iOS's identifierForVendor, but sufficient for traceability per CU-06.
        const string key = "sgr.device_id";
        var existing = Preferences.Default.Get(key, string.Empty);
        if (string.IsNullOrEmpty(existing))
        {
            existing = $"win-{Guid.NewGuid():N}";
            Preferences.Default.Set(key, existing);
        }
        _cachedId = existing;
        return existing;
    }
}

public sealed class SgrApiException : Exception
{
    public int Status { get; }
    public string Title { get; }
    public string? ErrorCode { get; }

    public SgrApiException(int status, string title, string detail, string? errorCode) : base(detail)
    {
        Status = status;
        Title = title;
        ErrorCode = errorCode;
    }
}

public sealed record LoginResponseDto(
    string AccessToken,
    DateTime ExpiresAtUtc,
    Guid UserId,
    string Role,
    Guid? AreaId);

public sealed record SurveyDto(
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

public sealed record CreateSurveyDto(
    Guid Id,
    string Name,
    string? Description,
    Guid? TemplateVersionId,
    string? Tags,
    string Origin,
    string? DeviceId,
    DateTime? TimestampOriginal);

public sealed record ProblemDetailsDto(
    string? Title,
    int? Status,
    string? Detail,
    string? Instance,
    string? ErrorCode);

public abstract record LoginResult
{
    public sealed record Ok(LoginResponseDto Response) : LoginResult;
    public sealed record InvalidCredentials() : LoginResult;
    public sealed record Forbidden(string Detail) : LoginResult;
    public sealed record Error(string Detail) : LoginResult;
}
