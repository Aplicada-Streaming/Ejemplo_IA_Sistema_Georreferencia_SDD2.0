using Sgr.Domain.Common;

namespace Sgr.Domain.Surveys;

public sealed class Survey : Entity
{
    public string Name { get; private set; } = default!;
    public string? Description { get; private set; }
    public Guid AreaId { get; private set; }
    public Guid OwnerId { get; private set; }
    public Guid TemplateVersionId { get; private set; }
    public string Status { get; private set; } = default!;
    public string? Tags { get; private set; }
    public DateTime UpdatedAt { get; private set; }
    public DateTime? ClosedAt { get; private set; }
    public bool IsDeleted { get; private set; }
    public DateTime? DeletedAt { get; private set; }

    private Survey() { }

    public static Survey Create(
        Guid id,
        string name,
        string? description,
        Guid areaId,
        Guid ownerId,
        Guid templateVersionId,
        string? tags,
        DateTime createdAt)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Survey name is required.", nameof(name));
        if (id == Guid.Empty)
            throw new ArgumentException("Survey id is required.", nameof(id));

        return new Survey
        {
            Id = id,
            Name = name.Trim(),
            Description = description?.Trim(),
            AreaId = areaId,
            OwnerId = ownerId,
            TemplateVersionId = templateVersionId,
            Status = SurveyStatus.Abierto,
            Tags = tags,
            CreatedAt = createdAt,
            UpdatedAt = createdAt,
            ClosedAt = null,
            IsDeleted = false,
            DeletedAt = null,
        };
    }

    /// <summary>
    /// Cierra el relevamiento. Operación idempotente: si ya está cerrado no hace nada.
    /// El cierre fija ClosedAt y bloquea futuras modificaciones (RN-09 — pendiente
    /// integrar con sync para rechazar eventos PostClose).
    /// </summary>
    public void Close(DateTime when)
    {
        if (Status == SurveyStatus.Cerrado) return;
        if (Status == SurveyStatus.EliminadoLogico)
            throw new InvalidOperationException("No se puede cerrar un relevamiento eliminado.");
        Status = SurveyStatus.Cerrado;
        ClosedAt = when;
        UpdatedAt = when;
    }

    /// <summary>
    /// Reabre un relevamiento cerrado. Usado por la resolución de conflictos post-cierre
    /// (DT-S9.1): si el admin/jefe decide aplicar una captura tardía, primero se reabre
    /// y luego se reaplica el evento via sync.
    /// </summary>
    public void Reopen(DateTime when)
    {
        if (Status == SurveyStatus.Abierto) return;
        if (Status == SurveyStatus.EliminadoLogico)
            throw new InvalidOperationException("No se puede reabrir un relevamiento eliminado.");
        Status = SurveyStatus.Abierto;
        ClosedAt = null;
        UpdatedAt = when;
    }
}
