using Sgr.Domain.Common;

namespace Sgr.Domain.Templates;

public sealed class Template : Entity
{
    public string Name { get; private set; } = default!;
    public Guid? ParentTemplateId { get; private set; }
    public bool IsRoot { get; private set; }
    public bool IsDeletable { get; private set; }

    private Template() { }

    public static Template CreateRoot(Guid id, string name, DateTime createdAt) =>
        new()
        {
            Id = id,
            Name = name.Trim(),
            ParentTemplateId = null,
            IsRoot = true,
            IsDeletable = false,
            CreatedAt = createdAt,
        };

    public static Template CreateChild(Guid id, string name, Guid parentTemplateId, DateTime createdAt)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Template name is required.", nameof(name));

        return new Template
        {
            Id = id,
            Name = name.Trim(),
            ParentTemplateId = parentTemplateId,
            IsRoot = false,
            IsDeletable = true,
            CreatedAt = createdAt,
        };
    }
}
