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

    private static string ResolveBackendUrl()
    {
        // Default: localhost:5000.
        //   - Windows: backend corre en la misma máquina.
        //   - Android (USB + ADB): se establece `adb reverse tcp:5000 tcp:5000` antes
        //     de iniciar la app, lo cual hace que `localhost:5000` desde el dispositivo
        //     se reenvíe al PC. Ver scripts/deploy-mobile.bat.
        //   - Android emulator: lo mismo que arriba; alternativamente podría usarse 10.0.2.2.
        //   - iOS simulator: localhost también funciona.
        //
        // Para correr contra una IP de WiFi, sobreescribir esta URL en la entrada de la app
        // o exponer la BackendApiOptions vía Preferences.
        return "http://localhost:5000";
    }
}
