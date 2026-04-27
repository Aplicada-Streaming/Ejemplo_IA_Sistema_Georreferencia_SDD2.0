namespace Sgr.Frontend.Mobile.Camera;

/// <summary>
/// Puerto para la captura de fotos vía cámara del dispositivo.
///
/// La captura usa el flujo system-camera (MediaPicker en MAUI) que abre la app de
/// cámara nativa, no una cámara embebida. La foto resultante se copia inmediatamente
/// a <c>AppDataDirectory/photos/&lt;photoId&gt;.&lt;ext&gt;</c> para que sobreviva al
/// ciclo de vida del temp folder y al outbox para reintentos.
/// </summary>
public interface ICameraCaptureService
{
    /// <summary>
    /// Captura una foto. <paramref name="photoId"/> determina el nombre del archivo
    /// destino para que sea estable across reintentos del outbox.
    /// </summary>
    Task<CameraCaptureResult> CaptureAsync(Guid photoId, CancellationToken ct = default);
}
