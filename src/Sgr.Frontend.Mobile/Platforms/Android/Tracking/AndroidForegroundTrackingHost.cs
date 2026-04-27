using Android.Content;
using Android.OS;
using Microsoft.Extensions.Logging;
using Sgr.Frontend.Mobile.Geolocation;

namespace Sgr.Frontend.Mobile.Platforms.Android.Tracking;

/// <summary>
/// Implementación Android de <see cref="IForegroundTrackingHost"/>.
/// Levanta / detiene <see cref="TrackingForegroundService"/> via intent.
///
/// POST_NOTIFICATIONS (Android 13+) y FOREGROUND_SERVICE_LOCATION (Android 14+):
/// se piden runtime desde la UI antes de invocar Start. Si el usuario lo niega,
/// el FG service igual arranca pero la notificación no se mostrará y Android
/// puede matar el service más rápido — degrada al comportamiento pre-fix.
/// </summary>
public sealed class AndroidForegroundTrackingHost : IForegroundTrackingHost
{
    private readonly ILogger<AndroidForegroundTrackingHost> _logger;

    public AndroidForegroundTrackingHost(ILogger<AndroidForegroundTrackingHost> logger)
    {
        _logger = logger;
    }

    public void Start(string title, string description)
    {
        try
        {
            var ctx = global::Android.App.Application.Context;
            var intent = new Intent(ctx, typeof(TrackingForegroundService));
            intent.PutExtra(TrackingForegroundService.ExtraTitle, title);
            intent.PutExtra(TrackingForegroundService.ExtraDescription, description);

            // OperatingSystem.IsAndroidVersionAtLeast es el version-gate que CA1416 reconoce
            // (Build.VERSION.SdkInt funciona en runtime pero el analizador no lo silencia).
            if (OperatingSystem.IsAndroidVersionAtLeast(26))
                ctx.StartForegroundService(intent);
            else
                ctx.StartService(intent);

            _logger.LogInformation("Foreground tracking service started.");
        }
        catch (Exception ex)
        {
            // No tiramos: el tracker debe seguir andando aunque el FG host falle
            // (ver doc de IForegroundTrackingHost). Quedamos en modo "foreground only".
            _logger.LogWarning(ex, "No pude arrancar el foreground service. Tracking sigue en foreground only.");
        }
    }

    public void Stop()
    {
        try
        {
            var ctx = global::Android.App.Application.Context;
            var intent = new Intent(ctx, typeof(TrackingForegroundService));
            ctx.StopService(intent);
            _logger.LogInformation("Foreground tracking service stopped.");
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error parando foreground service (best-effort).");
        }
    }
}
