using System.Text.Json;
using System.Text.Json.Serialization;

namespace Sgr.Modules.Templates.Application;

/// <summary>
/// Definición de un campo dentro de una <c>VersiónDePlantilla</c>.
///
/// Mapea contra el JSON del seeder:
/// <code>
///   { "key": "fecha_inspeccion", "label": "Fecha de inspección",
///     "type": "fecha", "required": true }
/// </code>
///
/// Para campos de tipo <c>selección</c>, <see cref="Options"/> trae las opciones
/// válidas. Para todos los demás tipos es null.
/// </summary>
public sealed record FieldDefinition(
    string Key,
    string Label,
    string Type,
    bool Required,
    IReadOnlyList<string>? Options = null);

public static class FieldType
{
    public const string Texto = "texto";
    public const string Numero = "numero";
    public const string Fecha = "fecha";
    public const string Booleano = "booleano";
    public const string Seleccion = "selección";

    public static readonly IReadOnlySet<string> All =
        new HashSet<string> { Texto, Numero, Fecha, Booleano, Seleccion };

    public static bool IsValid(string s) => All.Contains(s);
}

/// <summary>
/// Parámetros de captura asociados a una <c>VersiónDePlantilla</c> (PROJECT-BRIEF Sec. 7.4).
/// Cuando una nueva versión se publica, todos los relevamientos que la usan empiezan a
/// regir por estos valores.
///
/// Mapea exacto al JSON del seeder. Los nombres se mantienen en snake_case en el JSON
/// pero acá usamos PascalCase con converters de System.Text.Json.
/// </summary>
public sealed record CaptureParams(
    [property: JsonPropertyName("gps_timeout_seconds")] int GpsTimeoutSeconds,
    [property: JsonPropertyName("gps_accuracy_threshold_m")] double GpsAccuracyThresholdM,
    [property: JsonPropertyName("allow_continue_with_low_accuracy")] bool AllowContinueWithLowAccuracy,
    [property: JsonPropertyName("allow_manual_coordinates_entry_mobile")] bool AllowManualCoordinatesEntryMobile,
    [property: JsonPropertyName("movil_radius_m")] double MovilRadiusM,
    [property: JsonPropertyName("photo_max_long_side_px")] int PhotoMaxLongSidePx,
    [property: JsonPropertyName("photo_jpeg_quality")] int PhotoJpegQuality,
    [property: JsonPropertyName("photo_target_format")] string PhotoTargetFormat,
    [property: JsonPropertyName("photo_keep_original")] bool PhotoKeepOriginal,
    [property: JsonPropertyName("photo_generate_thumbnail")] bool PhotoGenerateThumbnail,
    [property: JsonPropertyName("photo_strip_sensitive_exif")] bool PhotoStripSensitiveExif,
    [property: JsonPropertyName("merge_radius_m")] double MergeRadiusM,
    [property: JsonPropertyName("merge_time_window_hours")] int MergeTimeWindowHours);

/// <summary>Helpers de (de)serialización del schema persistido en JSON.</summary>
public static class TemplateSchemaJson
{
    private static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web)
    {
        PropertyNameCaseInsensitive = true,
    };

    public static IReadOnlyList<FieldDefinition> ParseFields(string json)
    {
        if (string.IsNullOrWhiteSpace(json)) return Array.Empty<FieldDefinition>();
        return JsonSerializer.Deserialize<List<FieldDefinition>>(json, Options)
            ?? new List<FieldDefinition>();
    }

    public static CaptureParams ParseCaptureParams(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
            throw new ArgumentException("CaptureParamsJson vacío.", nameof(json));
        return JsonSerializer.Deserialize<CaptureParams>(json, Options)
            ?? throw new InvalidOperationException("No pude deserializar CaptureParams.");
    }
}
