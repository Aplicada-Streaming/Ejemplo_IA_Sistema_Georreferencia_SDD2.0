namespace Sgr.Domain.Common;

public abstract class Entity
{
    public Guid Id { get; protected set; }
    public DateTime CreatedAt { get; protected set; }

    protected Entity() { }

    protected Entity(Guid id, DateTime createdAt)
    {
        Id = id;
        CreatedAt = createdAt;
    }

    public override bool Equals(object? obj) =>
        obj is Entity other && Id == other.Id && GetType() == other.GetType();

    public override int GetHashCode() => HashCode.Combine(GetType(), Id);
}
