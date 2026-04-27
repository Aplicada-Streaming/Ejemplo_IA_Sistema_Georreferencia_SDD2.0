using Microsoft.Extensions.Logging;
using Microsoft.Maui.Devices.Sensors;

namespace Sgr.Frontend.Mobile.Geolocation;

public sealed class MauiGeolocationService : IGeolocationService
{
    private readonly ILogger<MauiGeolocationService> _logger;

    public MauiGeolocationService(ILogger<MauiGeolocationService> logger)
    {
        _logger = logger;
    }

    public async Task<GeolocationResult> AcquireAsync(
        GeolocationParams parameters,
        IProgress<GeolocationProgress>? onProgress = null,
        CancellationToken ct = default)
    {
        // S0 — verificar/solicitar permisos.
        onProgress?.Report(GeolocationProgress.CheckingPermissions);

        var permissionResult = await EnsureLocationPermissionAsync();
        if (permissionResult is not null) return permissionResult;

        // S2 — obtener fix con timeout. Reportamos progreso para que la UI pueda mostrar contador.
        onProgress?.Report(GeolocationProgress.AcquiringFix);

        try
        {
            var request = new GeolocationRequest(GeolocationAccuracy.Best,
                TimeSpan.FromSeconds(parameters.TimeoutSeconds))
            {
                RequestFullAccuracy = true,
            };

            // GetLocationAsync respeta el timeout interno; igual usamos cancelación cooperativa.
            using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            linkedCts.CancelAfter(TimeSpan.FromSeconds(parameters.TimeoutSeconds + 1));

            var location = await Microsoft.Maui.Devices.Sensors.Geolocation.Default.GetLocationAsync(
                request, linkedCts.Token);

            if (location is null)
                return new GeolocationResult.Timeout(parameters.TimeoutSeconds);

            var accuracyM = (decimal)(location.Accuracy ?? double.MaxValue);
            var lat = (decimal)location.Latitude;
            var lng = (decimal)location.Longitude;
            var capturedAt = location.Timestamp.UtcDateTime;

            // S3-LOWACC: precisión peor que el threshold.
            if (accuracyM > (decimal)parameters.AccuracyThresholdMeters)
            {
                _logger.LogInformation(
                    "GPS fix con precisión baja: {Acc}m > threshold {Thr}m",
                    accuracyM, parameters.AccuracyThresholdMeters);
                return new GeolocationResult.LowAccuracy(
                    lat, lng, accuracyM, (decimal)parameters.AccuracyThresholdMeters, capturedAt);
            }

            return new GeolocationResult.Ok(lat, lng, accuracyM, capturedAt);
        }
        catch (FeatureNotSupportedException)
        {
            return new GeolocationResult.Error("Este dispositivo no soporta geolocalización.");
        }
        catch (FeatureNotEnabledException)
        {
            // S3-NOSIGNAL: GPS desactivado.
            return new GeolocationResult.NoSignal();
        }
        catch (PermissionException)
        {
            return new GeolocationResult.PermissionDenied(IsPermanent: false);
        }
        catch (TaskCanceledException) when (ct.IsCancellationRequested)
        {
            return new GeolocationResult.Cancelled();
        }
        catch (TaskCanceledException)
        {
            return new GeolocationResult.Timeout(parameters.TimeoutSeconds);
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            return new GeolocationResult.Cancelled();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "GPS acquisition failed unexpectedly.");
            return new GeolocationResult.Error(ex.Message);
        }
    }

    public void OpenSystemSettings()
    {
        try
        {
            AppInfo.Current.ShowSettingsUI();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "ShowSettingsUI failed — silently ignored.");
        }
    }

    /// <summary>S0 + S1: pide permiso y, si denegado, devuelve el resultado terminal correspondiente.</summary>
    /// <remarks>
    /// Permissions.* requiere UI thread. El tracker corre en background (Task.Run en el loop GPS),
    /// así que envolvemos cada llamada en MainThread.InvokeOnMainThreadAsync — sin esto el tracker
    /// crashea con "Permission request must be invoked on main thread" la primera vez que pide fix.
    /// </remarks>
    private static async Task<GeolocationResult?> EnsureLocationPermissionAsync()
    {
        var status = await MainThread.InvokeOnMainThreadAsync(
            () => Permissions.CheckStatusAsync<Permissions.LocationWhenInUse>());
        if (status == PermissionStatus.Granted) return null;

        status = await MainThread.InvokeOnMainThreadAsync(
            () => Permissions.RequestAsync<Permissions.LocationWhenInUse>());

        if (status == PermissionStatus.Granted) return null;

        var isPermanent = await MainThread.InvokeOnMainThreadAsync(
            () => !Permissions.ShouldShowRationale<Permissions.LocationWhenInUse>());
        return new GeolocationResult.PermissionDenied(isPermanent);
    }
}
