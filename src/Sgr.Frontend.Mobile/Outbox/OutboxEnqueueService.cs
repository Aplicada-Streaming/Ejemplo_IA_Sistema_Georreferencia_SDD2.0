using System.Text.Json;
using Sgr.Frontend.Mobile.Api;
using Sgr.Frontend.Mobile.Auth;

namespace Sgr.Frontend.Mobile.Outbox;

/// <summary>
/// Helper de alto nivel: encola eventos en el outbox listos para que el
/// drainer los push al backend.
/// </summary>
public interface IOutboxEnqueueService
{
    /// <summary>
    /// Crea un evento <c>point.created</c> y lo persiste en el outbox local.
    /// La operación es atómica: o se persiste localmente o falla la llamada.
    /// </summary>
    Task<string> EnqueuePointCreatedAsync(
        Guid surveyId,
        Guid pointId,
        decimal latitude,
        decimal longitude,
        decimal? accuracyM,
        string captureMode,
        DateTime timestamp);
}

public sealed class OutboxEnqueueService : IOutboxEnqueueService
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly IPendingOperationStore _store;
    private readonly IMobileTokenStore _tokens;
    private readonly IDeviceIdProvider _device;

    public OutboxEnqueueService(
        IPendingOperationStore store,
        IMobileTokenStore tokens,
        IDeviceIdProvider device)
    {
        _store = store;
        _tokens = tokens;
        _device = device;
    }

    public async Task<string> EnqueuePointCreatedAsync(
        Guid surveyId,
        Guid pointId,
        decimal latitude,
        decimal longitude,
        decimal? accuracyM,
        string captureMode,
        DateTime timestamp)
    {
        var session = await _tokens.GetAsync()
            ?? throw new InvalidOperationException("Sesión no disponible para encolar eventos.");

        var ev = new SyncEventDto(
            EventId: Guid.NewGuid(),
            EntityType: "point",
            EntityId: pointId,
            EventType: "created",
            Field: null,
            OldValueJson: null,
            NewValueJson: JsonSerializer.Serialize(new
            {
                surveyId,
                latitude,
                longitude,
                accuracyM,
                captureMode,
            }, JsonOptions),
            AuthorId: session.UserId,
            Origin: "mobile_capture",
            DeviceId: _device.GetDeviceId(),
            TimestampOriginal: timestamp);

        var eventsJson = JsonSerializer.Serialize(new[] { ev }, JsonOptions);
        return await _store.EnqueueAsync(surveyId.ToString(), eventsJson, DateTime.UtcNow);
    }
}
