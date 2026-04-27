using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.OS;
using AndroidX.Core.App;

namespace Sgr.Frontend.Mobile.Platforms.Android.Tracking;

/// <summary>
/// Foreground service que mantiene viva la app durante el trazado continuo (modo móvil).
/// Mostrando una notificación persistente, Android compromete a NO matar el proceso
/// y a NO restringirle el acceso al GPS aunque el usuario salga de la app.
///
/// El service en sí mismo no hace el polling — eso lo sigue haciendo el
/// <c>MauiContinuousGpsTracker</c> con su loop interno. La única responsabilidad
/// del service es <strong>existir</strong> y <strong>mostrar la notif</strong>;
/// con eso le dice al SO "este proceso está activo, no me throttees".
/// </summary>
[Service(
    Name = "vialidad.sgr.TrackingForegroundService",
    Exported = false,
    ForegroundServiceType = ForegroundService.TypeLocation)]
public sealed class TrackingForegroundService : Service
{
    public const string ChannelId = "sgr.tracking";
    public const string ChannelName = "Trazado SGR";
    public const int NotificationId = 1001;

    public const string ExtraTitle = "title";
    public const string ExtraDescription = "description";

    public override IBinder? OnBind(Intent? intent) => null;   // no exponemos binder

    public override StartCommandResult OnStartCommand(Intent? intent, StartCommandFlags flags, int startId)
    {
        var title = intent?.GetStringExtra(ExtraTitle) ?? "SGR · Trazado activo";
        var description = intent?.GetStringExtra(ExtraDescription)
            ?? "Capturando puntos GPS en segundo plano.";

        EnsureChannel();
        var notification = BuildNotification(title, description);

        // Sticky para que si el SO mata el service por OOM extremo, lo relance al haber memoria.
        // En API 34+ ForegroundServiceType debe coincidir con el declarado en el atributo Service.
        if (Build.VERSION.SdkInt >= BuildVersionCodes.Q)
            StartForeground(NotificationId, notification, ForegroundService.TypeLocation);
        else
            StartForeground(NotificationId, notification);

        return StartCommandResult.Sticky;
    }

    public override void OnDestroy()
    {
        // Borramos la notif al detener el service.
        if (Build.VERSION.SdkInt >= BuildVersionCodes.N)
            StopForeground(StopForegroundFlags.Remove);
        else
            #pragma warning disable CA1422  // member obsoleto antes de N
            StopForeground(removeNotification: true);
            #pragma warning restore CA1422
        base.OnDestroy();
    }

    private void EnsureChannel()
    {
        if (Build.VERSION.SdkInt < BuildVersionCodes.O) return;   // pre-Oreo no usa channels
        var manager = (NotificationManager)GetSystemService(NotificationService)!;
        if (manager.GetNotificationChannel(ChannelId) is not null) return;

        var channel = new NotificationChannel(ChannelId, ChannelName, NotificationImportance.Low)
        {
            Description = "Indica que el trazado de puntos GPS está activo.",
            LockscreenVisibility = NotificationVisibility.Public,
        };
        channel.SetShowBadge(false);
        manager.CreateNotificationChannel(channel);
    }

    private Notification BuildNotification(string title, string description)
    {
        // Tap → vuelve a abrir la app (pantalla de trazado vía deep link al menos al "/").
        var openAppIntent = PackageManager?.GetLaunchIntentForPackage(PackageName!);
        var pi = openAppIntent is null ? null : PendingIntent.GetActivity(
            this, 0, openAppIntent,
            PendingIntentFlags.UpdateCurrent | PendingIntentFlags.Immutable);

        // Icon system-provided (siempre existe en cualquier API level).
        // Una mejora futura: un icon monocromo dedicado en Resources/drawable
        // se ve mejor en el statusbar (los icons de mipmap pueden mostrarse
        // como cuadrado blanco en algunos OEMs).
        var builder = new NotificationCompat.Builder(this, ChannelId)
            .SetContentTitle(title)
            .SetContentText(description)
            .SetSmallIcon(global::Android.Resource.Drawable.IcDialogInfo)
            .SetOngoing(true)
            .SetCategory(NotificationCompat.CategoryService)
            .SetVisibility(NotificationCompat.VisibilityPublic)
            .SetPriority(NotificationCompat.PriorityLow);
        if (pi is not null) builder.SetContentIntent(pi);
        return builder.Build();
    }
}
