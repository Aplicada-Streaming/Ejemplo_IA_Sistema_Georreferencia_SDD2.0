namespace Sgr.Frontend.Mobile.Outbox;

public interface IPendingOperationStore
{
    Task InitializeAsync();

    Task<string> EnqueueAsync(string surveyId, string eventsJson, DateTime now);

    Task<IReadOnlyList<PendingOperation>> GetReadyAsync(DateTime now, int limit = 20);

    Task<IReadOnlyList<PendingOperation>> GetAllAsync();

    Task<PendingOperation?> GetByIdAsync(string id);

    Task MarkInFlightAsync(string id, DateTime now);

    Task MarkSentAsync(string id, DateTime now);

    Task MarkErrorAsync(string id, string error, int attempts, DateTime? nextRetryAt, bool terminal, DateTime now);

    Task<int> CountAsync(string status);

    /// <summary>Devuelve a 'pending' las operaciones en estado 'error' con NextRetryAt en el futuro.
    /// Permite forzar un retry inmediato desde la UI.</summary>
    Task RescheduleNowAsync(string id, DateTime now);
}
