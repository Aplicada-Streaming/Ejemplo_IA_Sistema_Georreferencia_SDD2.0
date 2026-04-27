namespace Sgr.Frontend.Mobile.Geolocation;

/// <summary>
/// Puerto del servicio de geolocalización para Slice 1 de US-06.
///
/// El método <see cref="AcquireAsync"/> ejecuta la máquina S0→S2→S3 completa y devuelve
/// un <see cref="GeolocationResult"/> con el estado terminal. El callback <c>onProgress</c>
/// permite que la UI muestre los estados transitorios (S0 verificando permisos, S2 contador de segundos).
///
/// Implementaciones:
///   • <see cref="MauiGeolocationService"/> (Android/Windows/iOS) — usa Microsoft.Maui.Devices.Geolocation
///   • Fakes para tests unitarios (no incluido en Slice 3.1)
/// </summary>
public interface IGeolocationService
{
    Task<GeolocationResult> AcquireAsync(
        GeolocationParams parameters,
        IProgress<GeolocationProgress>? onProgress = null,
        CancellationToken ct = default);

    /// <summary>Abre la pantalla de configuración de permisos del SO.
    /// Útil cuando <see cref="GeolocationResult.PermissionDenied.IsPermanent"/> es true.</summary>
    void OpenSystemSettings();
}
