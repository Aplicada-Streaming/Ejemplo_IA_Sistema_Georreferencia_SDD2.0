using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.Extensions.Logging;
using MudBlazor.Services;
using Sgr.Frontend.Mobile.Api;
using Sgr.Frontend.Mobile.Auth;
using Sgr.Frontend.Mobile.Camera;
using Sgr.Frontend.Mobile.Geolocation;
using Sgr.Frontend.Mobile.Outbox;

namespace Sgr.Frontend.Mobile;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        builder
            .UseMauiApp<App>()
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
            });

        builder.Services.AddMauiBlazorWebView();
        builder.Services.AddMudServices();

        // Backend API URL — picked up at app start. For Android emulator the host's
        // 'localhost' is reachable at 10.0.2.2 (default emulator setup).
        var backendOptions = new BackendApiOptions
        {
            BaseUrl = ResolveBackendUrl(),
        };
        builder.Services.AddSingleton(backendOptions);

        builder.Services.AddSingleton<IDeviceIdProvider, MauiDeviceIdProvider>();
        builder.Services.AddSingleton<IMobileTokenStore, SecureMobileTokenStore>();

        builder.Services.AddHttpClient<ISgrApiClient, SgrApiClient>((sp, http) =>
        {
            var opts = sp.GetRequiredService<BackendApiOptions>();
            http.BaseAddress = new Uri(opts.BaseUrl);
        });

        // Auth state propagated to Blazor pages.
        builder.Services.AddScoped<MobileAuthenticationStateProvider>();
        builder.Services.AddScoped<AuthenticationStateProvider>(
            sp => sp.GetRequiredService<MobileAuthenticationStateProvider>());
        builder.Services.AddAuthorizationCore();
        builder.Services.AddCascadingAuthenticationState();

        // Outbox + drainer (US-03).
        builder.Services.AddSingleton<IPendingOperationStore, SqlitePendingOperationStore>();
        builder.Services.AddScoped<IOutboxEnqueueService, OutboxEnqueueService>();
        builder.Services.AddSingleton<IOutboxDrainer, OutboxDrainer>();

        // GPS / geolocalización (E.3.1, US-06 parcial: máquina S0-S3 sin foto/mapa).
        builder.Services.AddSingleton<IGeolocationService, MauiGeolocationService>();

        // Tracker continuo para modo móvil (E.3.5). Singleton: una sola instancia
        // viva en la app; la lógica de Start/Stop la maneja la página de trazado.
        builder.Services.AddSingleton<IContinuousGpsTracker, MauiContinuousGpsTracker>();

        // DT-bg-tracking: foreground service host. Android usa el real, otros
        // platforms el no-op. La selección es compile-time vía symbol ANDROID.
#if ANDROID
        builder.Services.AddSingleton<IForegroundTrackingHost,
            Platforms.Android.Tracking.AndroidForegroundTrackingHost>();
#else
        builder.Services.AddSingleton<IForegroundTrackingHost, NoopForegroundTrackingHost>();
#endif

        // Resolver de profile desde la plantilla del survey (E.5.a). Scoped porque
        // depende de ISgrApiClient que también es scoped en BlazorWebView.
        builder.Services.AddScoped<ICaptureProfileResolver, RemoteCaptureProfileResolver>();

        // Cámara + foto (E.3.3, US-06).
        builder.Services.AddSingleton<ICameraCaptureService, MauiCameraCaptureService>();

#if DEBUG
        builder.Services.AddBlazorWebViewDeveloperTools();
        builder.Logging.AddDebug();
#endif

        return builder.Build();
    }

    private const string DefaultBackendUrl = "http://localhost:5000";

    /// <summary>
    /// La URL del backend se lee de <c>Resources/Raw/sgr-config.json</c> (un MauiAsset).
    /// Ese archivo lo patchea <c>scripts/docker/host-windows-prod/publish-mobile.bat</c>
    /// con la IP del host destino antes de compilar el APK que se distribuye al cliente.
    /// En dev local con USB + adb reverse el asset trae <c>localhost:5000</c>.
    /// Ante cualquier error de lectura caemos al default para no romper el arranque.
    /// </summary>
    private static string ResolveBackendUrl()
    {
        try
        {
            using var stream = FileSystem.OpenAppPackageFileAsync("sgr-config.json")
                .GetAwaiter().GetResult();
            using var doc = System.Text.Json.JsonDocument.Parse(stream);
            if (doc.RootElement.TryGetProperty("backendBaseUrl", out var prop) &&
                prop.ValueKind == System.Text.Json.JsonValueKind.String)
            {
                var url = prop.GetString();
                if (!string.IsNullOrWhiteSpace(url)) return url;
            }
        }
        catch
        {
            // El asset puede faltar en builds que excluyen Resources/Raw o si la deserialización
            // falla. Fallback silencioso al default.
        }
        return DefaultBackendUrl;
    }
}
