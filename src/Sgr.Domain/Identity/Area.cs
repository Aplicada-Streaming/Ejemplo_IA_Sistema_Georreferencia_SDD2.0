using Sgr.Domain.Common;

namespace Sgr.Domain.Identity;

public sealed class Area : Entity
{
    public string Name { get; private set; } = default!;
    public string? Description { get; private set; }

    private Area() { }

    public Area(Guid id, string name, string? description, DateTime createdAt)
        : base(id, createdAt)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Area name is required.", nameof(name));
        Name = name.Trim();
        Description = description;
    }
}
