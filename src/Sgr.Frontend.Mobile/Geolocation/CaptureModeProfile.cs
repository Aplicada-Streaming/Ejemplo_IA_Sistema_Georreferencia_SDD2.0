namespace Sgr.Frontend.Mobile.Geolocation;

/// <summary>
/// Perfil de configuración por modo de captura (PROJECT-BRIEF Sec. 7.5).
/// En Slice 5 estos valores van a venir de la <c>VersiónDePlantilla</c> activa
/// del relevamiento; por ahora son constantes y se eligen por
/// <see cref="CaptureModeProfiles"/> según el modo.
///
/// SamplingIntervalSeconds: sólo en modo móvil, cada cuántos segundos pedir un nuevo fix.
/// DiscardRadiusMeters: sólo en modo móvil, radio dinámico bajo el cual un nuevo fix se
/// descarta como duplicado del último aceptado. Evita "spam" de puntos cuando el
/// dispositivo está casi quieto o el GPS oscila por jitter.
/// </summary>
public sealed record CaptureModeProfile(
    string Mode,
    int TimeoutSeconds,
    double AccuracyThresholdMeters,
    bool AllowContinueWithLowAccuracy,
    int? SamplingIntervalSeconds,
    double? DiscardRadiusMeters);

public static class CaptureModeProfiles
{
    /// <summary>
    /// Captura puntual: el usuario decide cuándo pedir GPS. Threshold estricto (50m).
    /// No tiene sampling automático ni descarte por radio.
    /// </summary>
    public static readonly CaptureModeProfile Detenido = new(
        Mode: "detenido",
        TimeoutSeconds: 30,
        AccuracyThresholdMeters: 50.0,
        AllowContinueWithLowAccuracy: true,
        SamplingIntervalSeconds: null,
        DiscardRadiusMeters: null);

    /// <summary>
    /// Trazado continuo: cada 5s se pide un fix; si está dentro de 25m del último
    /// aceptado se descarta. Threshold relajado (100m) porque el contexto en
    /// movimiento típicamente da menos precisión.
    /// </summary>
    public static readonly CaptureModeProfile Movil = new(
        Mode: "movil",
        TimeoutSeconds: 15,
        AccuracyThresholdMeters: 100.0,
        AllowContinueWithLowAccuracy: true,
        SamplingIntervalSeconds: 5,
        DiscardRadiusMeters: 25.0);

    public static CaptureModeProfile ForMode(string mode) => mode switch
    {
        "detenido" => Detenido,
        "movil" => Movil,
        _ => throw new ArgumentException($"Modo de captura desconocido: '{mode}'", nameof(mode)),
    };
}
