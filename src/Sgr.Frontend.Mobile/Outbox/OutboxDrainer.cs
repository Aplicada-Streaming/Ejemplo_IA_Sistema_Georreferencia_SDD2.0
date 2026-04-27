using System.Net.Http;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Sgr.Frontend.Mobile.Api;

namespace Sgr.Frontend.Mobile.Outbox;

public interface IOutboxDrainer
{
    /// <summary>Drena el outbox una vez. Devuelve cantidad de operaciones enviadas exitosamente.</summary>
    Task<DrainResult> DrainOnceAsync(CancellationToken ct = default);

    /// <summary>Inicia el loop background. Idempotente: si ya está corriendo, no hace nada.</summary>
    Task StartAsync(CancellationToken ct);

    /// <summary>Detiene el loop background.</summary>
    Task StopAsync();
}

public sealed record DrainResult(int Sent, int FailedTransient, int FailedTerminal);

public sealed class OutboxDrainer : IOutboxDrainer, IDisposable
{
    private readonly IPendingOperationStore _store;
    private readonly ISgrApiClient _api;
    private readonly ILogger<OutboxDrainer> _logger;
    private readonly TimeSpan _pollInterval = TimeSpan.FromSeconds(5);

    private CancellationTokenSource? _loopCts;
    private Task? _loopTask;
    private readonly SemaphoreSlim _drainLock = new(1, 1);

    public OutboxDrainer(IPendingOperationStore store, ISgrApiClient api, ILogger<OutboxDrainer> logger)
    {
        _store = store;
        _api = api;
        _logger = logger;
    }

    public async Task StartAsync(CancellationToken ct)
    {
        if (_loopTask is not null) return;

        await _store.InitializeAsync();

        _loopCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        _loopTask = Task.Run(() => RunLoopAsync(_loopCts.Token), _loopCts.Token);
        _logger.LogInformation("OutboxDrainer started.");
    }

    public async Task StopAsync()
    {
        if (_loopCts is null || _loopTask is null) return;
        try
        {
            _loopCts.Cancel();
            await _loopTask;
        }
        catch (OperationCanceledException) { }
        finally
        {
            _loopCts.Dispose();
            _loopCts = null;
            _loopTask = null;
        }
        _logger.LogInformation("OutboxDrainer stopped.");
    }

    public async Task<DrainResult> DrainOnceAsync(CancellationToken ct = default)
    {
        await _drainLock.WaitAsync(ct);
        try
        {
            int sent = 0, failedTransient = 0, failedTerminal = 0;
            var now = DateTime.UtcNow;
            var ready = await _store.GetReadyAsync(now, limit: 20);

            foreach (var op in ready)
            {
                ct.ThrowIfCancellationRequested();
                var (s, ft, term) = await TryPushAsync(op, ct);
                sent += s; failedTransient += ft; failedTerminal += term;
            }

            return new DrainResult(sent, failedTransient, failedTerminal);
        }
        finally
        {
            _drainLock.Release();
        }
    }

    private async Task<(int sent, int failedTransient, int failedTerminal)> TryPushAsync(
        PendingOperation op, CancellationToken ct)
    {
        var now = DateTime.UtcNow;
        await _store.MarkInFlightAsync(op.Id, now);

        try
        {
            switch (op.Kind)
            {
                case OperationKind.PhotoUpload:
                    await PushPhotoAsync(op, ct);
                    break;
                case OperationKind.SyncEvent:
                default:
                    await PushSyncEventsAsync(op, ct);
                    break;
            }
            await _store.MarkSentAsync(op.Id, DateTime.UtcNow);
            return (1, 0, 0);
        }
        catch (HttpRequestException ex)
        {
            return await HandleErrorAsync(op, ex, transient: true, ct);
        }
        catch (TaskCanceledException ex)
        {
            return await HandleErrorAsync(op, ex, transient: true, ct);
        }
        catch (SgrApiException ex) when (ex.Status >= 500 || ex.Status == 408 || ex.Status == 429)
        {
            return await HandleErrorAsync(op, ex, transient: true, ct);
        }
        catch (SgrApiException ex)
        {
            // 4xx no transitorio (excepto 408/429): error de payload o autorización. Terminal de inmediato.
            return await HandleErrorAsync(op, ex, transient: false, ct, forceTerminal: true);
        }
        catch (FileNotFoundException ex)
        {
            // Foto local desaparecida (limpieza externa, app reinstalada, etc.). No-recoverable.
            return await HandleErrorAsync(op, ex, transient: false, ct, forceTerminal: true);
        }
        catch (Exception ex)
        {
            return await HandleErrorAsync(op, ex, transient: true, ct);
        }
    }

    private async Task PushSyncEventsAsync(PendingOperation op, CancellationToken ct)
    {
        var response = await _api.PushEventsRawAsync(op.EventsJson, ct);
        _logger.LogInformation(
            "Pushed outbox op {OpId}: received={Received}, applied={Applied}, skipped={Skipped}",
            op.Id, response.Received, response.Applied, response.Skipped);
    }

    private async Task PushPhotoAsync(PendingOperation op, CancellationToken ct)
    {
        if (string.IsNullOrEmpty(op.LocalFilePath))
            throw new InvalidOperationException("photo_upload sin LocalFilePath.");
        if (!File.Exists(op.LocalFilePath))
            throw new FileNotFoundException("Foto local desaparecida.", op.LocalFilePath);

        var meta = JsonSerializer.Deserialize<PhotoUploadMetadata>(op.EventsJson,
            new JsonSerializerOptions(JsonSerializerDefaults.Web))
            ?? throw new InvalidOperationException("Metadata de foto inválida en outbox.");

        await using (var fs = new FileStream(op.LocalFilePath, FileMode.Open, FileAccess.Read, FileShare.Read))
        {
            var result = await _api.UploadPhotoAsync(
                pointId: meta.PointId,
                photoId: meta.PhotoId,
                content: fs,
                fileName: meta.FileName,
                mimeType: meta.MimeType,
                comment: meta.Comment,
                origin: meta.Origin,
                ct: ct);

            _logger.LogInformation(
                "Photo subida {OpId}: photoId={PhotoId}, adapter={Adapter}, size={Size} bytes, hash={Hash}",
                op.Id, result.Id, result.AdapterName, result.SizeBytes, result.ContentHash);
        }

        // Best-effort cleanup. Si falla la borrada el outbox marca Sent igual: peor caso quedan
        // bytes en AppDataDirectory que se limpiarán al desinstalar la app.
        try { File.Delete(op.LocalFilePath); }
        catch (Exception ex) { _logger.LogWarning(ex, "No pude borrar foto local {Path}.", op.LocalFilePath); }
    }

    private async Task<(int sent, int failedTransient, int failedTerminal)> HandleErrorAsync(
        PendingOperation op, Exception ex, bool transient, CancellationToken ct, bool forceTerminal = false)
    {
        var attempts = op.Attempts + 1;
        var now = DateTime.UtcNow;
        var nextRetry = forceTerminal ? null : RetryPolicy.NextRetryAt(attempts, now);
        var terminal = forceTerminal || nextRetry is null;

        await _store.MarkErrorAsync(op.Id, ex.Message, attempts, nextRetry, terminal, now);

        if (terminal)
        {
            _logger.LogWarning(ex, "Outbox op {OpId} terminal-error after {Attempts} attempts.", op.Id, attempts);
            return (0, 0, 1);
        }
        else
        {
            _logger.LogWarning(
                "Outbox op {OpId} transient error (attempt {Attempts}/{Max}). Next retry at {Next}: {Reason}",
                op.Id, attempts, RetryPolicy.MaxAttempts, nextRetry, ex.Message);
            return (0, 1, 0);
        }
    }

    private async Task RunLoopAsync(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            try
            {
                await DrainOnceAsync(ct);
            }
            catch (OperationCanceledException) { return; }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Outbox drain loop error.");
            }

            try { await Task.Delay(_pollInterval, ct); }
            catch (OperationCanceledException) { return; }
        }
    }

    public void Dispose()
    {
        _loopCts?.Cancel();
        _loopCts?.Dispose();
        _drainLock.Dispose();
    }
}
