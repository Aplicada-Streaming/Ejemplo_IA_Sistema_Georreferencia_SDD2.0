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

    /// <summary>
    /// E.3.4: lista los puntos de un relevamiento para renderearlos en el mapa.
    /// El backend filtra por visibilidad según el rol del usuario.
    /// </summary>
    Task<IReadOnlyList<PointDto>> ListPointsAsync(Guid surveyId, CancellationToken ct = default);

    /// <summary>
    /// E.5.a: trae la versión de plantilla activa para el relevamiento — el cuerpo
    /// se devuelve crudo (string JSON) porque sólo lo necesitamos para cachear y
    /// extraer <c>captureParams</c>; los DTOs strongly-typed están del lado server.
    /// </summary>
    Task<string> GetSurveyTemplateVersionRawAsync(Guid surveyId, CancellationToken ct = default);

    /// <summary>
    /// US-04: push de un batch de eventos al backend. Idempotente por EventId (RN-06).
    /// El cuerpo se serializa tal cual desde <paramref name="eventsJson"/> para que el outbox
    /// pueda persistir el JSON exacto que se envió y reusarlo en reintentos sin reserialización.
    /// </summary>
    Task<SyncPushResponseDto> PushEventsRawAsync(string eventsJson, CancellationToken ct = default);

    /// <summary>
    /// E.3.3: subida multipart de una foto capturada. Idempotente por <paramref name="photoId"/>
    /// (si el server ya tiene esa foto, se considera Sent). El stream se consume una sola vez
    /// y queda al cargo del caller cerrarlo.
    /// </summary>
    Task<PhotoCreatedDto> UploadPhotoAsync(
        Guid pointId,
        Guid photoId,
        Stream content,
        string fileName,
        string mimeType,
        string? comment,
        string origin,
        CancellationToken ct = default);
}

public sealed class SgrApiClient : ISgrApiClient
{
    private readonly HttpClient _http;
    private readonly IMobileTokenStore _store;
    private readonly IDeviceIdProvider _device;
    private readonly MobileAuthenticationStateProvider _auth;

    public SgrApiClient(
        HttpClient http,
        IMobileTokenStore store,
        IDeviceIdProvider device,
        MobileAuthenticationStateProvider auth)
    {
        _http = http;
        _store = store;
        _device = device;
        _auth = auth;
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
        await ThrowIfUnauthorizedAsync(response);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<IReadOnlyList<SurveyDto>>(cancellationToken: ct)
            ?? Array.Empty<SurveyDto>();
    }

    public async Task<string> GetSurveyTemplateVersionRawAsync(Guid surveyId, CancellationToken ct = default)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, $"/api/v1/surveys/{surveyId}/template-version");
        await AttachAuthAsync(request);

        using var response = await _http.SendAsync(request, ct);
        await ThrowIfUnauthorizedAsync(response);
        if (!response.IsSuccessStatusCode)
        {
            var problem = await ReadProblemAsync(response, ct);
            throw new SgrApiException(
                (int)response.StatusCode,
                problem?.Title ?? response.ReasonPhrase ?? "Error",
                problem?.Detail ?? "No pude obtener la plantilla del relevamiento.",
                problem?.ErrorCode);
        }
        return await response.Content.ReadAsStringAsync(ct);
    }

    public async Task<IReadOnlyList<PointDto>> ListPointsAsync(Guid surveyId, CancellationToken ct = default)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, $"/api/v1/surveys/{surveyId}/points");
        await AttachAuthAsync(request);

        using var response = await _http.SendAsync(request, ct);
        await ThrowIfUnauthorizedAsync(response);
        if (!response.IsSuccessStatusCode)
        {
            var problem = await ReadProblemAsync(response, ct);
            throw new SgrApiException(
                (int)response.StatusCode,
                problem?.Title ?? response.ReasonPhrase ?? "Error",
                problem?.Detail ?? "No pude listar los puntos.",
                problem?.ErrorCode);
        }

        return await response.Content.ReadFromJsonAsync<IReadOnlyList<PointDto>>(
            new JsonSerializerOptions(JsonSerializerDefaults.Web), ct)
            ?? Array.Empty<PointDto>();
    }

    public async Task<SurveyDto> CreateSurveyAsync(CreateSurveyDto requestDto, CancellationToken ct = default)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/v1/surveys")
        {
            Content = JsonContent.Create(requestDto),
        };
        await AttachAuthAsync(request);

        using var response = await _http.SendAsync(request, ct);
        await ThrowIfUnauthorizedAsync(response);
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

    public async Task<SyncPushResponseDto> PushEventsRawAsync(string eventsJson, CancellationToken ct = default)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/v1/sync/push")
        {
            Content = new StringContent(
                "{\"events\":" + eventsJson + "}",
                System.Text.Encoding.UTF8,
                "application/json"),
        };
        await AttachAuthAsync(request);

        using var response = await _http.SendAsync(request, ct);
        await ThrowIfUnauthorizedAsync(response);
        if (!response.IsSuccessStatusCode)
        {
            var problem = await ReadProblemAsync(response, ct);
            throw new SgrApiException(
                (int)response.StatusCode,
                problem?.Title ?? response.ReasonPhrase ?? "Error",
                problem?.Detail ?? "Sync push falló.",
                problem?.ErrorCode);
        }

        return await response.Content.ReadFromJsonAsync<SyncPushResponseDto>(
            new JsonSerializerOptions(JsonSerializerDefaults.Web)
            {
                Converters = { new System.Text.Json.Serialization.JsonStringEnumConverter() },
            }, ct) ?? throw new InvalidOperationException("Respuesta del sync vacía.");
    }

    public async Task<PhotoCreatedDto> UploadPhotoAsync(
        Guid pointId,
        Guid photoId,
        Stream content,
        string fileName,
        string mimeType,
        string? comment,
        string origin,
        CancellationToken ct = default)
    {
        using var form = new MultipartFormDataContent();
        var fileContent = new StreamContent(content);
        fileContent.Headers.ContentType = new MediaTypeHeaderValue(mimeType);
        form.Add(fileContent, "File", fileName);
        form.Add(new StringContent(pointId.ToString()), "PointId");
        form.Add(new StringContent(photoId.ToString()), "PhotoId");
        if (!string.IsNullOrWhiteSpace(comment))
            form.Add(new StringContent(comment), "Comment");
        form.Add(new StringContent(origin), "Origin");

        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/v1/photos") { Content = form };
        await AttachAuthAsync(request);

        using var response = await _http.SendAsync(request, ct);
        await ThrowIfUnauthorizedAsync(response);
        if (!response.IsSuccessStatusCode)
        {
            var problem = await ReadProblemAsync(response, ct);
            throw new SgrApiException(
                (int)response.StatusCode,
                problem?.Title ?? response.ReasonPhrase ?? "Error",
                problem?.Detail ?? "Upload de foto falló.",
                problem?.ErrorCode);
        }

        return await response.Content.ReadFromJsonAsync<PhotoCreatedDto>(
            new JsonSerializerOptions(JsonSerializerDefaults.Web), ct)
            ?? throw new InvalidOperationException("Respuesta vacía del upload.");
    }

    private async Task AttachAuthAsync(HttpRequestMessage request)
    {
        var session = await _store.GetAsync();
        if (session is null) throw await SessionExpiredAsync();

        // ROPC bearer sin refresh token (DT-01): cuando expira, hay que volver a loguearse.
        // Detectamos el caso acá para no hacer el roundtrip y caer en un 401 después.
        if (session.ExpiresAtUtc <= DateTime.UtcNow)
            throw await SessionExpiredAsync();

        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", session.AccessToken);
    }

    private async Task<SgrApiException> SessionExpiredAsync()
    {
        // Limpia el token + notifica al AuthenticationStateProvider para que [Authorize]
        // dispare el RedirectToLogin en la próxima evaluación del routing.
        await _auth.SignOutAsync();
        return new SgrApiException(
            (int)HttpStatusCode.Unauthorized,
            "Sesión expirada",
            "Tu sesión expiró. Volvé a iniciar sesión.",
            errorCode: "session_expired");
    }

    /// <summary>Detecta 401 del servidor (token revocado / inválido del lado server) y
    /// dispara el mismo flujo de signout. Llamar antes de los handlers de error específicos.</summary>
    private async Task ThrowIfUnauthorizedAsync(HttpResponseMessage response)
    {
        if (response.StatusCode == HttpStatusCode.Unauthorized)
            throw await SessionExpiredAsync();
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

/// <summary>
/// Mismo schema que el SyncEventDto del backend (Sgr.Modules.Sync.Application.SyncEventDto).
/// Lo replicamos acá para evitar atar la app móvil al ensamblado del backend.
/// </summary>
public sealed record SyncEventDto(
    Guid EventId,
    string EntityType,
    Guid EntityId,
    string EventType,
    string? Field,
    string? OldValueJson,
    string? NewValueJson,
    Guid AuthorId,
    string Origin,
    string? DeviceId,
    DateTime TimestampOriginal);

public sealed record SyncPushResponseDto(
    int Received,
    int Applied,
    int Skipped,
    IReadOnlyList<SyncEventResultDto> Results);

public sealed record SyncEventResultDto(
    Guid EventId,
    string Outcome,    // "Applied" | "Idempotent" | "LosesLWW" | "LosesOwnerPrecedence" | "RejectedPostClose" | ...
    string? Message);

/// <summary>Espejo del PhotoCreatedDto del backend (PhotosController).</summary>
public sealed record PhotoCreatedDto(
    Guid Id,
    string AdapterName,
    string AdapterRef,
    long SizeBytes,
    string ContentHash);

/// <summary>Espejo del PointDto del backend (Sgr.Modules.Surveys.Application).</summary>
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
