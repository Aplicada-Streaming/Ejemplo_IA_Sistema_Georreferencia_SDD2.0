namespace Sgr.Frontend.Mobile.Geolocation;

/// <summary>
/// Resuelve los <see cref="CaptureModeProfile"/> para un relevamiento usando los
/// <c>captureParams</c> publicados en su <c>VersiónDePlantilla</c> (E.5.a).
///
/// Política de caché y offline:
///   1. Intenta bajar el JSON del backend.
///   2. Si trae OK, lo guarda en <see cref="Microsoft.Maui.Storage.Preferences"/>
///      keyed por <c>surveyId</c> (TTL infinito; las versiones publicadas son
///      inmutables RN-05).
///   3. Si la red falla, lee del cache local.
///   4. Si tampoco hay cache (primer uso offline del relevamiento), cae a los
///      <see cref="CaptureModeProfiles"/> hardcoded como último recurso.
/// </summary>
public interface ICaptureProfileResolver
{
    Task<CaptureModeProfile> ResolveAsync(Guid surveyId, string mode, CancellationToken ct = default);

    /// <summary>
    /// E.5.b — Devuelve la lista de campos definidos por la plantilla del relevamiento,
    /// con el mismo policy de cache: red → preferences → vacío como último recurso.
    /// </summary>
    Task<IReadOnlyList<TemplateField>> ResolveFieldsAsync(Guid surveyId, CancellationToken ct = default);
}

/// <summary>Espejo móvil de FieldDefinition (sin atar la app al ensamblado del backend).</summary>
public sealed record TemplateField(
    string Key,
    string Label,
    string Type,
    bool Required,
    IReadOnlyList<string>? Options);
