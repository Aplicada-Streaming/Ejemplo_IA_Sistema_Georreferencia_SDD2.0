namespace Sgr.Frontend.Mobile.Geolocation;

/// <summary>
/// Parámetros de captura GPS, alineados con la sección "Parámetros configurables por plantilla"
/// de PROJECT-BRIEF Sec. 7.4. En Slice 1 (US-06 todavía pendiente) los valores los pasa
/// el caller; en Slice 5 los va a leer de la <c>VersiónDePlantilla</c> del relevamiento.
/// </summary>
public sealed record GeolocationParams(
    int TimeoutSeconds = 30,
    double AccuracyThresholdMeters = 50.0,
    bool AllowContinueWithLowAccuracy = true,
    bool AllowManualEntry = false);
