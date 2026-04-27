namespace Sgr.Frontend.Mobile.Camera;

/// <summary>
/// Resultado de capturar una foto desde la cámara del dispositivo.
///
/// Discriminated union:
///   • Ok            — foto capturada y persistida en una ubicación estable
///   • Cancelled     — usuario canceló desde la app de cámara
///   • PermissionDenied — el permiso de cámara fue denegado (transitorio o "no preguntar más")
///   • NotSupported  — el dispositivo no tiene cámara o el SO no soporta MediaPicker
///   • Error         — falla genérica (storage lleno, IO error, etc.)
/// </summary>
public abstract record CameraCaptureResult
{
    /// <summary>Foto guardada en <see cref="LocalFilePath"/> (absoluta, dentro de AppDataDirectory).</summary>
    public sealed record Ok(
        string LocalFilePath,
        string FileName,
        string MimeType,
        long SizeBytes,
        DateTime CapturedAtUtc) : CameraCaptureResult;

    public sealed record Cancelled() : CameraCaptureResult;

    public sealed record PermissionDenied(bool IsPermanent) : CameraCaptureResult;

    public sealed record NotSupported() : CameraCaptureResult;

    public sealed record Error(string Message) : CameraCaptureResult;
}
