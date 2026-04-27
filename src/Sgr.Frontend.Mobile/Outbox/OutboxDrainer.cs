using System.Net.Http;
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
            var response = await _api.PushEventsRawAsync(op.EventsJson, ct);
            await _store.MarkSentAsync(op.Id, DateTime.UtcNow);
            _logger.LogInformation(
                "Pushed outbox op {OpId}: received={Received}, applied={Applied}, skipped={Skipped}",
                op.Id, response.Received, response.Applied, response.Skipped);
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
        catch (Exception ex)
        {
            return await HandleErrorAsync(op, ex, transient: true, ct);
        }
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
