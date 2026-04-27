using Sgr.Domain.Common;

namespace Sgr.Domain.SystemConfig;

/// <summary>
/// Configuración de sistema persistida en DB. Modelo key/value para que la
/// admin pueda cambiar parámetros runtime sin tocar el deploy (US-17 / CU-02).
///
/// El valor puede ser opaco para el dominio: en general es un JSON serializado
/// por la capa de aplicación. Si contiene credenciales, el caller cifra
/// antes de pasar el string por <see cref="ValueJson"/> (DataProtection).
/// </summary>
public sealed class SystemConfigEntry : Entity
{
    public string Key { get; private set; } = default!;
    public string ValueJson { get; private set; } = default!;
    public Guid UpdatedBy { get; private set; }
    public DateTime UpdatedAt { get; private set; }

    private SystemConfigEntry() { }

    public static SystemConfigEntry Create(
        Guid id,
        string key,
        string valueJson,
        Guid updatedBy,
        DateTime nowUtc)
    {
        if (id == Guid.Empty) throw new ArgumentException("Id required.", nameof(id));
        if (string.IsNullOrWhiteSpace(key)) throw new ArgumentException("Key required.", nameof(key));
        if (string.IsNullOrWhiteSpace(valueJson)) throw new ArgumentException("ValueJson required.", nameof(valueJson));
        if (updatedBy == Guid.Empty) throw new ArgumentException("UpdatedBy required.", nameof(updatedBy));

        return new SystemConfigEntry
        {
            Id = id,
            Key = key,
            ValueJson = valueJson,
            UpdatedBy = updatedBy,
            UpdatedAt = nowUtc,
            CreatedAt = nowUtc,
        };
    }

    public void UpdateValue(string valueJson, Guid updatedBy, DateTime nowUtc)
    {
        if (string.IsNullOrWhiteSpace(valueJson)) throw new ArgumentException("ValueJson required.", nameof(valueJson));
        if (updatedBy == Guid.Empty) throw new ArgumentException("UpdatedBy required.", nameof(updatedBy));
        ValueJson = valueJson;
        UpdatedBy = updatedBy;
        UpdatedAt = nowUtc;
    }
}

public static class SystemConfigKeys
{
    /// <summary>Clave para la config activa de storage. Valor: JSON con adapter + config cifrada.</summary>
    public const string StorageActive = "storage.active";
}
