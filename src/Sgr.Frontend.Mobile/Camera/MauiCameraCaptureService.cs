using Microsoft.Extensions.Logging;
using Microsoft.Maui.Media;

namespace Sgr.Frontend.Mobile.Camera;

public sealed class MauiCameraCaptureService : ICameraCaptureService
{
    private readonly ILogger<MauiCameraCaptureService> _logger;

    public MauiCameraCaptureService(ILogger<MauiCameraCaptureService> logger)
    {
        _logger = logger;
    }

    public async Task<CameraCaptureResult> CaptureAsync(Guid photoId, CancellationToken ct = default)
    {
        if (!MediaPicker.Default.IsCaptureSupported)
            return new CameraCaptureResult.NotSupported();

        var permissionResult = await EnsureCameraPermissionAsync();
        if (permissionResult is not null) return permissionResult;

        try
        {
            var photo = await MediaPicker.Default.CapturePhotoAsync();
            if (photo is null)
                return new CameraCaptureResult.Cancelled();

            var ext = Path.GetExtension(photo.FileName);
            if (string.IsNullOrEmpty(ext)) ext = ".jpg";
            var fileName = $"{photoId:N}{ext}";

            var photosDir = Path.Combine(FileSystem.AppDataDirectory, "photos");
            Directory.CreateDirectory(photosDir);
            var destPath = Path.Combine(photosDir, fileName);

            // Copiamos a permanent storage. El temp file de MediaPicker puede ser limpiado por el SO.
            long sizeBytes;
            await using (var src = await photo.OpenReadAsync())
            await using (var dst = new FileStream(destPath, FileMode.Create, FileAccess.Write, FileShare.None))
            {
                await src.CopyToAsync(dst, ct);
                sizeBytes = dst.Length;
            }

            var mimeType = photo.ContentType ?? "image/jpeg";
            return new CameraCaptureResult.Ok(
                LocalFilePath: destPath,
                FileName: photo.FileName,
                MimeType: mimeType,
                SizeBytes: sizeBytes,
                CapturedAtUtc: DateTime.UtcNow);
        }
        catch (FeatureNotSupportedException)
        {
            return new CameraCaptureResult.NotSupported();
        }
        catch (PermissionException)
        {
            return new CameraCaptureResult.PermissionDenied(IsPermanent: false);
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            return new CameraCaptureResult.Cancelled();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Camera capture failed.");
            return new CameraCaptureResult.Error(ex.Message);
        }
    }

    private static async Task<CameraCaptureResult?> EnsureCameraPermissionAsync()
    {
        var status = await Permissions.CheckStatusAsync<Permissions.Camera>();
        if (status == PermissionStatus.Granted) return null;

        status = await Permissions.RequestAsync<Permissions.Camera>();
        if (status == PermissionStatus.Granted) return null;

        var isPermanent = !Permissions.ShouldShowRationale<Permissions.Camera>();
        return new CameraCaptureResult.PermissionDenied(isPermanent);
    }
}
