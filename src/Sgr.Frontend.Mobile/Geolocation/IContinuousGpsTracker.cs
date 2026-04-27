namespace Sgr.Frontend.Mobile.Geolocation;

/// <summary>
/// Loop de captura continua para modo "móvil" (PROJECT-BRIEF Sec. 7.5).
///
/// Diseño:
///   • <see cref="StartAsync"/> arranca un Task interno que cada
///     <c>SamplingIntervalSeconds</c> pide un fix vía <see cref="IGeolocationService"/>.
///   • Cada fix se filtra: si <c>accuracy &gt; threshold</c> → descarta;
///     si está dentro del <c>DiscardRadius</c> del último ACEPTADO → descarta;
///     si pasa los filtros → invoca <c>OnAccepted</c>.
///   • Errores transitorios (timeout, no signal puntual) NO detienen el loop:
///     se reportan via <c>OnError</c> y el loop sigue al próximo intervalo.
///   • <see cref="StopAsync"/> es idempotente y siempre cancela el loop.
///
/// Vida útil del tracker = vida útil de la app. Si el usuario navega fuera de la
/// página de tracking debe llamar a Stop manualmente — no hay foreground service
/// (queda fuera de scope para Slice 1; ver DT-bg-tracking).
/// </summary>
public interface IContinuousGpsTracker
{
    bool IsRunning { get; }

    /// <summary>
    /// Arranca el tracking. Si ya está corriendo, lanza <see cref="InvalidOperationException"/>.
    /// </summary>
    Task StartAsync(CaptureModeProfile profile, TrackingCallbacks callbacks, CancellationToken ct);

    /// <summary>Detiene el tracking. Idempotente: si no está corriendo, no hace nada.</summary>
    Task StopAsync();
}

/// <summary>
/// Callbacks invocados por el tracker en su propio thread. Las implementaciones
/// son responsables de marshalling al UI thread (en Blazor: <c>InvokeAsync</c>).
/// </summary>
public sealed record TrackingCallbacks(
    Func<TrackedPoint, Task> OnAccepted,
    Func<TrackedPointDiscarded, Task>? OnDiscarded = null,
    Func<string, Task>? OnError = null);

public sealed record TrackedPoint(
    decimal Latitude,
    decimal Longitude,
    decimal AccuracyMeters,
    DateTime CapturedAtUtc,
    bool LowAccuracy);

public sealed record TrackedPointDiscarded(
    decimal Latitude,
    decimal Longitude,
    decimal AccuracyMeters,
    DiscardReason Reason,
    double? DistanceFromLastMeters);

public enum DiscardReason
{
    /// <summary>El fix tenía precisión peor que el threshold del modo.</summary>
    BelowAccuracyThreshold,
    /// <summary>El fix estaba dentro del radio dinámico del último aceptado.</summary>
    WithinDiscardRadius,
}
