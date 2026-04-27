using System.Text.Json;
using Microsoft.Extensions.Logging;
using Sgr.Frontend.Mobile.Api;

namespace Sgr.Frontend.Mobile.Geolocation;

public sealed class RemoteCaptureProfileResolver : ICaptureProfileResolver
{
    private const string CachePrefix = "sgr.captureparams.";
    private static readonly JsonSerializerOptions JsonOpts = new(JsonSerializerDefaults.Web);

    private readonly ISgrApiClient _api;
    private readonly ILogger<RemoteCaptureProfileResolver> _logger;

    public RemoteCaptureProfileResolver(ISgrApiClient api, ILogger<RemoteCaptureProfileResolver> logger)
    {
        _api = api;
        _logger = logger;
    }

    public async Task<CaptureModeProfile> ResolveAsync(
        Guid surveyId, string mode, CancellationToken ct = default)
    {
        var captureJson = await GetCaptureParamsJsonAsync(surveyId, ct);
        if (captureJson is null)
        {
            _logger.LogInformation(
                "Sin captureParams remotos ni cache para survey {SurveyId}, fallback a hardcoded.",
                surveyId);
            return CaptureModeProfiles.ForMode(mode);
        }

        try
        {
            var p = JsonSerializer.Deserialize<RawCaptureParams>(captureJson, JsonOpts)
                ?? throw new InvalidOperationException("captureParams JSON inválido.");
            return BuildProfile(mode, p);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex,
                "Falló parsear captureParams para survey {SurveyId}, fallback a hardcoded.",
                surveyId);
            return CaptureModeProfiles.ForMode(mode);
        }
    }

    /// <summary>
    /// Trae el JSON crudo de captureParams: primero la red, luego el cache.
    /// Devuelve null si no se pudo obtener de ningún lado.
    /// </summary>
    private async Task<string?> GetCaptureParamsJsonAsync(Guid surveyId, CancellationToken ct)
    {
        var cacheKey = CachePrefix + surveyId.ToString("N");

        try
        {
            var raw = await _api.GetSurveyTemplateVersionRawAsync(surveyId, ct);
            using var doc = JsonDocument.Parse(raw);
            if (doc.RootElement.TryGetProperty("captureParams", out var cp))
            {
                var captureJson = cp.GetRawText();
                Preferences.Default.Set(cacheKey, captureJson);
                return captureJson;
            }
        }
        catch (Exception ex)
        {
            _logger.LogInformation(ex,
                "No pude bajar plantilla de survey {SurveyId} (offline?). Intento cache.",
                surveyId);
        }

        var cached = Preferences.Default.Get(cacheKey, string.Empty);
        return string.IsNullOrEmpty(cached) ? null : cached;
    }

    private static CaptureModeProfile BuildProfile(string mode, RawCaptureParams p)
    {
        // El JSON publicado tiene una sola tanda de parámetros — no separa por modo.
        // Para "movil" tomamos los mismos valores pero con sampling/discardRadius
        // (movil_radius_m del JSON). Para "detenido" no hay sampling.
        return mode switch
        {
            "movil" => new CaptureModeProfile(
                Mode: "movil",
                TimeoutSeconds: p.GpsTimeoutSeconds,
                AccuracyThresholdMeters: p.GpsAccuracyThresholdM,
                AllowContinueWithLowAccuracy: p.AllowContinueWithLowAccuracy,
                SamplingIntervalSeconds: 5, // hardcoded por ahora; el JSON aún no expone esto
                DiscardRadiusMeters: p.MovilRadiusM),

            "detenido" => new CaptureModeProfile(
                Mode: "detenido",
                TimeoutSeconds: p.GpsTimeoutSeconds,
                AccuracyThresholdMeters: p.GpsAccuracyThresholdM,
                AllowContinueWithLowAccuracy: p.AllowContinueWithLowAccuracy,
                SamplingIntervalSeconds: null,
                DiscardRadiusMeters: null),

            _ => throw new ArgumentException($"Modo desconocido: '{mode}'", nameof(mode)),
        };
    }

    /// <summary>
    /// Espejo del JSON serializado en <c>TemplateVersion.CaptureParamsJson</c>.
    /// Las claves vienen en snake_case desde el seeder (PROJECT-BRIEF Sec. 7.4),
    /// por eso necesitamos <see cref="System.Text.Json.Serialization.JsonPropertyNameAttribute"/>
    /// — sin esto los valores quedaban todos en default y rompían la captura.
    /// </summary>
    private sealed record RawCaptureParams(
        [property: System.Text.Json.Serialization.JsonPropertyName("gps_timeout_seconds")] int GpsTimeoutSeconds,
        [property: System.Text.Json.Serialization.JsonPropertyName("gps_accuracy_threshold_m")] double GpsAccuracyThresholdM,
        [property: System.Text.Json.Serialization.JsonPropertyName("allow_continue_with_low_accuracy")] bool AllowContinueWithLowAccuracy,
        [property: System.Text.Json.Serialization.JsonPropertyName("allow_manual_coordinates_entry_mobile")] bool AllowManualCoordinatesEntryMobile,
        [property: System.Text.Json.Serialization.JsonPropertyName("movil_radius_m")] double MovilRadiusM,
        [property: System.Text.Json.Serialization.JsonPropertyName("photo_max_long_side_px")] int PhotoMaxLongSidePx,
        [property: System.Text.Json.Serialization.JsonPropertyName("photo_jpeg_quality")] int PhotoJpegQuality,
        [property: System.Text.Json.Serialization.JsonPropertyName("photo_target_format")] string PhotoTargetFormat,
        [property: System.Text.Json.Serialization.JsonPropertyName("photo_keep_original")] bool PhotoKeepOriginal,
        [property: System.Text.Json.Serialization.JsonPropertyName("photo_generate_thumbnail")] bool PhotoGenerateThumbnail,
        [property: System.Text.Json.Serialization.JsonPropertyName("photo_strip_sensitive_exif")] bool PhotoStripSensitiveExif,
        [property: System.Text.Json.Serialization.JsonPropertyName("merge_radius_m")] double MergeRadiusM,
        [property: System.Text.Json.Serialization.JsonPropertyName("merge_time_window_hours")] int MergeTimeWindowHours);
}
