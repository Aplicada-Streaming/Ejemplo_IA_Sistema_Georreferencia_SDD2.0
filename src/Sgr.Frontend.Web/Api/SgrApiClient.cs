using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Sgr.Frontend.Web.Auth;

namespace Sgr.Frontend.Web.Api;

public interface ISgrApiClient
{
    Task<LoginResult> LoginAsync(string email, string password, CancellationToken ct = default);
    Task<IReadOnlyList<SurveyDto>> ListSurveysAsync(string? statusFilter = null, CancellationToken ct = default);
    Task<SurveyDto> CreateSurveyAsync(CreateSurveyDto request, CancellationToken ct = default);

    // E.4.2: detalle, puntos, fotos, cierre.
    Task<SurveyDto> GetSurveyAsync(Guid surveyId, CancellationToken ct = default);
    Task<IReadOnlyList<PointDto>> ListPointsAsync(Guid surveyId, CancellationToken ct = default);
    Task<IReadOnlyList<PhotoSummaryDto>> ListPointPhotosAsync(Guid pointId, CancellationToken ct = default);
    Task<SurveyDto> CloseSurveyAsync(Guid surveyId, CancellationToken ct = default);

    /// <summary>URL absoluta al binario de una foto (incluye host del backend).
    /// El servidor sirve <c>image/*</c>; el browser maneja el GET con auth header
    /// vía <c>HttpClient</c>, no se puede usar directamente como <c>src</c> de un img tag.</summary>
    string GetPhotoContentUrl(Guid photoId);

    /// <summary>Baja el binario de una foto y devuelve el stream para mostrarlo.</summary>
    Task<HttpResponseMessage> DownloadPhotoAsync(Guid photoId, CancellationToken ct = default);
}

public sealed class SgrApiClient : ISgrApiClient
{
    private readonly HttpClient _http;
    private readonly IApiTokenAccessor _tokens;

    public SgrApiClient(HttpClient http, IApiTokenAccessor tokens)
    {
        _http = http;
        _tokens = tokens;
    }

    public async Task<LoginResult> LoginAsync(string email, string password, CancellationToken ct = default)
    {
        var response = await _http.PostAsJsonAsync("/api/v1/auth/login", new
        {
            email,
            password,
            client = "Web",
        }, ct);

        if (response.StatusCode == HttpStatusCode.Unauthorized)
            return new LoginResult.InvalidCredentials();
        if (response.StatusCode == HttpStatusCode.Forbidden)
        {
            var problem = await ReadProblemDetailsAsync(response, ct);
            return new LoginResult.Forbidden(problem?.Detail ?? "Acceso no permitido.");
        }

        if (!response.IsSuccessStatusCode)
        {
            var problem = await ReadProblemDetailsAsync(response, ct);
            return new LoginResult.Error(problem?.Detail ?? $"Error inesperado ({(int)response.StatusCode}).");
        }

        var body = await response.Content.ReadFromJsonAsync<LoginResponseDto>(cancellationToken: ct)
            ?? throw new InvalidOperationException("La respuesta de login estaba vacía.");
        return new LoginResult.Ok(body);
    }

    public async Task<IReadOnlyList<SurveyDto>> ListSurveysAsync(string? statusFilter = null, CancellationToken ct = default)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get,
            statusFilter is null ? "/api/v1/surveys" : $"/api/v1/surveys?status={Uri.EscapeDataString(statusFilter)}");
        AttachAuth(request);

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
        AttachAuth(request);

        using var response = await _http.SendAsync(request, ct);
        if (!response.IsSuccessStatusCode)
        {
            var problem = await ReadProblemDetailsAsync(response, ct);
            throw new SgrApiException(
                (int)response.StatusCode,
                problem?.Title ?? response.ReasonPhrase ?? "Error",
                problem?.Detail ?? "El servidor respondió con un error.",
                problem?.ErrorCode);
        }

        return await response.Content.ReadFromJsonAsync<SurveyDto>(cancellationToken: ct)
            ?? throw new InvalidOperationException("Respuesta del servidor vacía.");
    }

    public async Task<SurveyDto> GetSurveyAsync(Guid surveyId, CancellationToken ct = default)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, $"/api/v1/surveys/{surveyId}");
        AttachAuth(request);

        using var response = await _http.SendAsync(request, ct);
        if (!response.IsSuccessStatusCode)
            throw await BuildApiExceptionAsync(response, "No pude obtener el relevamiento.", ct);
        return await response.Content.ReadFromJsonAsync<SurveyDto>(cancellationToken: ct)
            ?? throw new InvalidOperationException("Respuesta vacía.");
    }

    public async Task<IReadOnlyList<PointDto>> ListPointsAsync(Guid surveyId, CancellationToken ct = default)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, $"/api/v1/surveys/{surveyId}/points");
        AttachAuth(request);

        using var response = await _http.SendAsync(request, ct);
        if (!response.IsSuccessStatusCode)
            throw await BuildApiExceptionAsync(response, "No pude listar los puntos.", ct);
        return await response.Content.ReadFromJsonAsync<IReadOnlyList<PointDto>>(cancellationToken: ct)
            ?? Array.Empty<PointDto>();
    }

    public async Task<IReadOnlyList<PhotoSummaryDto>> ListPointPhotosAsync(Guid pointId, CancellationToken ct = default)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, $"/api/v1/points/{pointId}/photos");
        AttachAuth(request);

        using var response = await _http.SendAsync(request, ct);
        if (!response.IsSuccessStatusCode)
            throw await BuildApiExceptionAsync(response, "No pude listar las fotos.", ct);
        return await response.Content.ReadFromJsonAsync<IReadOnlyList<PhotoSummaryDto>>(cancellationToken: ct)
            ?? Array.Empty<PhotoSummaryDto>();
    }

    public async Task<SurveyDto> CloseSurveyAsync(Guid surveyId, CancellationToken ct = default)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, $"/api/v1/surveys/{surveyId}/close");
        AttachAuth(request);

        using var response = await _http.SendAsync(request, ct);
        if (!response.IsSuccessStatusCode)
            throw await BuildApiExceptionAsync(response, "No pude cerrar el relevamiento.", ct);
        return await response.Content.ReadFromJsonAsync<SurveyDto>(cancellationToken: ct)
            ?? throw new InvalidOperationException("Respuesta vacía.");
    }

    public string GetPhotoContentUrl(Guid photoId) =>
        new Uri(_http.BaseAddress!, $"/api/v1/photos/{photoId}/content").ToString();

    public async Task<HttpResponseMessage> DownloadPhotoAsync(Guid photoId, CancellationToken ct = default)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, $"/api/v1/photos/{photoId}/content");
        AttachAuth(request);
        var response = await _http.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, ct);
        if (!response.IsSuccessStatusCode)
            throw await BuildApiExceptionAsync(response, "No pude bajar la foto.", ct);
        return response;
    }

    private async Task<SgrApiException> BuildApiExceptionAsync(HttpResponseMessage response, string defaultMsg, CancellationToken ct)
    {
        var problem = await ReadProblemDetailsAsync(response, ct);
        return new SgrApiException(
            (int)response.StatusCode,
            problem?.Title ?? response.ReasonPhrase ?? "Error",
            problem?.Detail ?? defaultMsg,
            problem?.ErrorCode);
    }

    private void AttachAuth(HttpRequestMessage request)
    {
        var token = _tokens.GetAccessToken();
        if (!string.IsNullOrWhiteSpace(token))
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
    }

    private static async Task<ProblemDetailsDto?> ReadProblemDetailsAsync(HttpResponseMessage response, CancellationToken ct)
    {
        try
        {
            return await response.Content.ReadFromJsonAsync<ProblemDetailsDto>(
                new JsonSerializerOptions(JsonSerializerDefaults.Web), ct);
        }
        catch { return null; }
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

/// <summary>Espejo de Sgr.Modules.Surveys.Application.PointDto.</summary>
public sealed record PointDto(
    Guid Id,
    Guid SurveyId,
    decimal Latitude,
    decimal Longitude,
    decimal? AccuracyM,
    string? Title,
    string? Description,
    string CaptureMode,
    DateTime CreatedAt,
    DateTime UpdatedAt);

/// <summary>Espejo de Sgr.Backend.Api.Controllers.PhotoSummaryDto.</summary>
public sealed record PhotoSummaryDto(
    Guid Id,
    Guid PointId,
    string AdapterName,
    long SizeBytes,
    string ContentHash,
    string? Comment,
    Guid CreatedBy,
    string Origin,
    DateTime CreatedAt);
