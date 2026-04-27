using Sgr.Domain.Common;

namespace Sgr.Domain.Points;

/// <summary>
/// Valor EAV de un campo de plantilla para un Punto (E.5.b / US-08).
///
/// Modelo deliberadamente simple: el value se almacena como JSON crudo en
/// <see cref="ValueJson"/>; el cliente sabe el tipo (texto/numero/fecha/booleano/
/// selección) consultando la VersiónDePlantilla del relevamiento.
///
/// Hay UNA fila por (PointId, FieldKey). Las actualizaciones modifican la fila
/// existente; no se versiona acá — el historial completo vive en AuditEvents
/// (RN-10) que dieta cada FieldUpdated por separado.
/// </summary>
public sealed class PointFieldValue : Entity
{
    public Guid PointId { get; private set; }
    public string FieldKey { get; private set; } = default!;
    public string? ValueJson { get; private set; }
    public DateTime UpdatedAt { get; private set; }
    public Guid UpdatedBy { get; private set; }

    private PointFieldValue() { }

    public static PointFieldValue Create(
        Guid id,
        Guid pointId,
        string fieldKey,
        string? valueJson,
        Guid updatedBy,
        DateTime updatedAt)
    {
        if (id == Guid.Empty) throw new ArgumentException("Id is required.", nameof(id));
        if (pointId == Guid.Empty) throw new ArgumentException("PointId is required.", nameof(pointId));
        if (string.IsNullOrWhiteSpace(fieldKey))
            throw new ArgumentException("FieldKey is required.", nameof(fieldKey));
        if (updatedBy == Guid.Empty) throw new ArgumentException("UpdatedBy is required.", nameof(updatedBy));

        return new PointFieldValue
        {
            Id = id,
            PointId = pointId,
            FieldKey = fieldKey,
            ValueJson = valueJson,
            UpdatedBy = updatedBy,
            UpdatedAt = updatedAt,
            CreatedAt = updatedAt,
        };
    }

    public void UpdateValue(string? valueJson, Guid updatedBy, DateTime updatedAt)
    {
        if (updatedBy == Guid.Empty) throw new ArgumentException("UpdatedBy is required.", nameof(updatedBy));
        ValueJson = valueJson;
        UpdatedBy = updatedBy;
        UpdatedAt = updatedAt;
    }
}
