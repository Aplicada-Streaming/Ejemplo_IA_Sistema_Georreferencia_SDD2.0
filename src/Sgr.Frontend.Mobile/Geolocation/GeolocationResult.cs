namespace Sgr.Frontend.Mobile.Geolocation;

/// <summary>
/// Resultado de la máquina de estados de captura GPS (PROJECT-BRIEF Sec. 7.2).
///
/// Estados modelados como discriminated union:
///   • S3-OK         — fix aceptable, devuelve coords
///   • S3-LowAcc     — fix con precisión peor que el threshold; UI puede ofrecer "Continuar igual"
///   • S3-Timeout    — no se obtuvo fix dentro del tiempo; UI ofrece "Reintentar"
///   • S3-NoSignal   — GPS apagado en el dispositivo
///   • S1-LocDenied  — el usuario denegó el permiso (transitoriamente o "no preguntar de nuevo")
///   • Cancelled     — el usuario canceló desde el diálogo
///   • Error         — falla genérica (hardware, red de assist GPS, etc.)
///
/// El estado <c>S0 (verificando permisos)</c> y <c>S2 (obteniendo GPS, contador)</c> son estados
/// transitorios visibles en la UI mientras se ejecuta el caso async — no aparecen como result final.
/// </summary>
public abstract record GeolocationResult
{
    /// <summary>S3-OK: fix con precisión dentro del threshold.</summary>
    public sealed record Ok(
        decimal Latitude,
        decimal Longitude,
        decimal AccuracyMeters,
        DateTime CapturedAtUtc) : GeolocationResult;

    /// <summary>S3-LowAcc: fix obtenido pero <c>accuracy &gt; threshold</c>. La UI decide si lo acepta.</summary>
    public sealed record LowAccuracy(
        decimal Latitude,
        decimal Longitude,
        decimal AccuracyMeters,
        decimal ThresholdMeters,
        DateTime CapturedAtUtc) : GeolocationResult;

    /// <summary>S3-Timeout: no llegó fix dentro del tiempo configurado.</summary>
    public sealed record Timeout(int TimeoutSeconds) : GeolocationResult;

    /// <summary>S3-NoSignal: GPS desactivado en el dispositivo (Settings → Ubicación).</summary>
    public sealed record NoSignal() : GeolocationResult;

    /// <summary>S1-LocDenied: permiso denegado. <c>IsPermanent</c> indica si el usuario eligió "no preguntar más".</summary>
    public sealed record PermissionDenied(bool IsPermanent) : GeolocationResult;

    /// <summary>Usuario canceló desde el diálogo (botón Cancelar).</summary>
    public sealed record Cancelled() : GeolocationResult;

    /// <summary>Error genérico (ex: Geolocation lanzó excepción inesperada).</summary>
    public sealed record Error(string Message) : GeolocationResult;
}

/// <summary>Estado transitorio para mostrar en la UI mientras la operación está en curso.</summary>
public enum GeolocationProgress
{
    Idle = 0,
    /// <summary>S0 — verificando/solicitando permisos.</summary>
    CheckingPermissions = 1,
    /// <summary>S2 — obteniendo GPS (mostrar contador).</summary>
    AcquiringFix = 2,
}
