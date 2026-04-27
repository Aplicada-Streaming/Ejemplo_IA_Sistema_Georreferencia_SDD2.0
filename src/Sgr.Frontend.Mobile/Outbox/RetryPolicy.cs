namespace Sgr.Frontend.Mobile.Outbox;

/// <summary>
/// Política exponencial de reintentos del outbox (RN-06 / PROJECT-BRIEF Sec. 5.2).
/// 1º intento: 5s · 2º: 15s · 3º: 1m · 4º: 5m · 5º: 15m · 6º: 30m · 7º (último): 1h.
/// Después de <see cref="MaxAttempts"/> intentos fallidos consecutivos, la operación
/// pasa a <c>terminal_error</c> y queda en el outbox para revisión humana / reintento manual.
/// </summary>
public static class RetryPolicy
{
    public const int MaxAttempts = 7;

    public static readonly TimeSpan[] Delays =
    {
        TimeSpan.FromSeconds(5),
        TimeSpan.FromSeconds(15),
        TimeSpan.FromMinutes(1),
        TimeSpan.FromMinutes(5),
        TimeSpan.FromMinutes(15),
        TimeSpan.FromMinutes(30),
        TimeSpan.FromHours(1),
    };

    /// <summary>
    /// Calcula <c>NextRetryAt</c> después del intento <paramref name="attempt"/> (1-indexed).
    /// Si el intento es ≥ <see cref="MaxAttempts"/>, devuelve null (no más reintentos).
    /// </summary>
    public static DateTime? NextRetryAt(int attempt, DateTime now)
    {
        if (attempt < 1)
            throw new ArgumentOutOfRangeException(nameof(attempt), attempt, "Attempt must be >= 1.");

        if (attempt > MaxAttempts) return null;

        // attempt N (1-based) usa Delays[N-1].
        var delay = Delays[Math.Min(attempt - 1, Delays.Length - 1)];
        return now + delay;
    }

    /// <summary>True si después de este número de intentos la operación pasa a terminal_error.</summary>
    public static bool IsTerminal(int attempts) => attempts >= MaxAttempts;
}
