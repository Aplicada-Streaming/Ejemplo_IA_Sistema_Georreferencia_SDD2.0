using Microsoft.Extensions.Logging;

namespace Sgr.Frontend.Mobile.Geolocation;

public sealed class MauiContinuousGpsTracker : IContinuousGpsTracker, IDisposable
{
    private readonly IGeolocationService _geo;
    private readonly ILogger<MauiContinuousGpsTracker> _logger;

    private CancellationTokenSource? _internalCts;
    private Task? _loopTask;
    private readonly SemaphoreSlim _stateLock = new(1, 1);

    public MauiContinuousGpsTracker(
        IGeolocationService geo,
        ILogger<MauiContinuousGpsTracker> logger)
    {
        _geo = geo;
        _logger = logger;
    }

    public bool IsRunning => _loopTask is { IsCompleted: false };

    public async Task StartAsync(
        CaptureModeProfile profile,
        TrackingCallbacks callbacks,
        CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(profile);
        ArgumentNullException.ThrowIfNull(callbacks);

        if (profile.SamplingIntervalSeconds is null || profile.DiscardRadiusMeters is null)
            throw new ArgumentException(
                "El perfil no es de modo continuo (faltan SamplingInterval / DiscardRadius).", nameof(profile));

        await _stateLock.WaitAsync(ct);
        try
        {
            if (IsRunning) throw new InvalidOperationException("El tracker ya está corriendo.");

            _internalCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            var token = _internalCts.Token;
            _loopTask = Task.Run(() => RunLoopAsync(profile, callbacks, token), token);
            _logger.LogInformation("Continuous GPS tracker started ({Mode}, every {Sec}s, discardRadius={R}m).",
                profile.Mode, profile.SamplingIntervalSeconds, profile.DiscardRadiusMeters);
        }
        finally
        {
            _stateLock.Release();
        }
    }

    public async Task StopAsync()
    {
        await _stateLock.WaitAsync();
        try
        {
            if (_internalCts is null) return;
            try { _internalCts.Cancel(); } catch { }
            if (_loopTask is not null)
            {
                try { await _loopTask; } catch (OperationCanceledException) { }
            }
            _internalCts.Dispose();
            _internalCts = null;
            _loopTask = null;
            _logger.LogInformation("Continuous GPS tracker stopped.");
        }
        finally
        {
            _stateLock.Release();
        }
    }

    private async Task RunLoopAsync(
        CaptureModeProfile profile,
        TrackingCallbacks callbacks,
        CancellationToken ct)
    {
        var threshold = (decimal)profile.AccuracyThresholdMeters;
        var discardRadius = profile.DiscardRadiusMeters!.Value;
        var interval = TimeSpan.FromSeconds(profile.SamplingIntervalSeconds!.Value);

        TrackedPoint? lastAccepted = null;

        while (!ct.IsCancellationRequested)
        {
            try
            {
                var fixResult = await _geo.AcquireAsync(profile.ToGeolocationParams(), onProgress: null, ct);
                var maybe = ExtractFix(fixResult);

                if (maybe is null)
                {
                    var msg = DescribeNonFixResult(fixResult);
                    if (msg is not null && callbacks.OnError is not null)
                        await callbacks.OnError(msg);
                }
                else
                {
                    var (lat, lng, acc, capturedAt, lowAcc) = maybe.Value;

                    // Filtro 1: precisión.
                    if (acc > threshold)
                    {
                        if (callbacks.OnDiscarded is not null)
                            await callbacks.OnDiscarded(new TrackedPointDiscarded(
                                lat, lng, acc, DiscardReason.BelowAccuracyThreshold, null));
                    }
                    else
                    {
                        // Filtro 2: radio dinámico vs último aceptado.
                        double? dist = null;
                        if (lastAccepted is not null)
                        {
                            dist = Distance.HaversineMeters(
                                (double)lastAccepted.Latitude, (double)lastAccepted.Longitude,
                                (double)lat, (double)lng);
                        }

                        if (dist is not null && dist.Value < discardRadius)
                        {
                            if (callbacks.OnDiscarded is not null)
                                await callbacks.OnDiscarded(new TrackedPointDiscarded(
                                    lat, lng, acc, DiscardReason.WithinDiscardRadius, dist));
                        }
                        else
                        {
                            var accepted = new TrackedPoint(lat, lng, acc, capturedAt, lowAcc);
                            lastAccepted = accepted;
                            await callbacks.OnAccepted(accepted);
                        }
                    }
                }
            }
            catch (OperationCanceledException) when (ct.IsCancellationRequested)
            {
                return;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Tracker iteration failed (continuing).");
                if (callbacks.OnError is not null)
                {
                    try { await callbacks.OnError(ex.Message); }
                    catch { /* el handler no debe tirar el loop */ }
                }
            }

            try { await Task.Delay(interval, ct); }
            catch (OperationCanceledException) { return; }
        }
    }

    private static (decimal lat, decimal lng, decimal acc, DateTime capturedAt, bool lowAcc)? ExtractFix(GeolocationResult r) => r switch
    {
        GeolocationResult.Ok ok => (ok.Latitude, ok.Longitude, ok.AccuracyMeters, ok.CapturedAtUtc, false),
        GeolocationResult.LowAccuracy low => (low.Latitude, low.Longitude, low.AccuracyMeters, low.CapturedAtUtc, true),
        _ => null,
    };

    private static string? DescribeNonFixResult(GeolocationResult r) => r switch
    {
        GeolocationResult.Timeout t => $"Timeout ({t.TimeoutSeconds}s) en este sample.",
        GeolocationResult.NoSignal => "GPS desactivado.",
        GeolocationResult.PermissionDenied => "Permiso de ubicación denegado.",
        GeolocationResult.Error err => $"Error GPS: {err.Message}",
        GeolocationResult.Cancelled => null,    // no es un error visible
        _ => null,
    };

    public void Dispose()
    {
        try { _internalCts?.Cancel(); } catch { }
        _internalCts?.Dispose();
        _stateLock.Dispose();
    }
}

internal static class CaptureModeProfileExtensions
{
    public static GeolocationParams ToGeolocationParams(this CaptureModeProfile p) => new(
        TimeoutSeconds: p.TimeoutSeconds,
        AccuracyThresholdMeters: p.AccuracyThresholdMeters,
        AllowContinueWithLowAccuracy: p.AllowContinueWithLowAccuracy,
        AllowManualEntry: false);
}
