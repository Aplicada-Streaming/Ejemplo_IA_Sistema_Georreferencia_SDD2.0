namespace Sgr.Domain.Audit;

/// <summary>
/// Append-only audit record (RN-10). Once persisted, never updated or deleted.
/// The DB enforces this with a trigger; the application layer never writes UPDATE/DELETE on this table.
/// </summary>
public sealed class AuditEvent
{
    public Guid Id { get; private set; }
    public string EntityType { get; private set; } = default!;
    public Guid EntityId { get; private set; }
    public string EventType { get; private set; } = default!;
    public string? FieldKey { get; private set; }
    public string? OldValueJson { get; private set; }
    public string? NewValueJson { get; private set; }
    public Guid AuthorId { get; private set; }
    public string Origin { get; private set; } = default!;
    public string? DeviceId { get; private set; }
    public DateTime TimestampOriginal { get; private set; }
    public DateTime AppliedAt { get; private set; }

    private AuditEvent() { }

    public static AuditEvent Create(
        Guid id,
        string entityType,
        Guid entityId,
        string eventType,
        string? fieldKey,
        string? oldValueJson,
        string? newValueJson,
        Guid authorId,
        string origin,
        string? deviceId,
        DateTime timestampOriginal,
        DateTime appliedAt)
    {
        if (!AuditEntityType.IsValid(entityType))
            throw new ArgumentException($"Invalid entity type '{entityType}'.", nameof(entityType));
        if (!AuditEventType.IsValid(eventType))
            throw new ArgumentException($"Invalid event type '{eventType}'.", nameof(eventType));
        if (!AuditOrigin.IsValid(origin))
            throw new ArgumentException($"Invalid origin '{origin}'.", nameof(origin));

        return new AuditEvent
        {
            Id = id,
            EntityType = entityType,
            EntityId = entityId,
            EventType = eventType,
            FieldKey = fieldKey,
            OldValueJson = oldValueJson,
            NewValueJson = newValueJson,
            AuthorId = authorId,
            Origin = origin,
            DeviceId = deviceId,
            TimestampOriginal = timestampOriginal,
            AppliedAt = appliedAt,
        };
    }
}
