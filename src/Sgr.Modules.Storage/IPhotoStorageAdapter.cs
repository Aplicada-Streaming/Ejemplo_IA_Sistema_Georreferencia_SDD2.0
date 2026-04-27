namespace Sgr.Modules.Storage;

/// <summary>
/// Puerto hexagonal para el storage de fotos (ADR-02).
/// Cada adapter implementa esta interfaz y se selecciona en runtime via
/// <see cref="IPhotoStorageAdapterFactory"/> según la <c>SystemConfig</c>.
/// </summary>
public interface IPhotoStorageAdapter
{
    /// <summary>Nombre lógico del adapter (local | s3 | ftp | sftp). Se persiste con cada Foto.</summary>
    string AdapterName { get; }

    Task<StoredPhotoRef> StoreAsync(Stream content, StorageHints hints, CancellationToken ct = default);
    Task<Stream> ReadAsync(string adapterRef, CancellationToken ct = default);
    Task DeleteAsync(string adapterRef, CancellationToken ct = default);

    /// <summary>Operación oportunista usada por el wizard del admin raíz para validar la conexión
    /// antes de persistir la configuración (CU-02).</summary>
    Task<bool> TestConnectionAsync(CancellationToken ct = default);
}

public sealed record StoredPhotoRef(
    string AdapterName,
    string AdapterRef,
    long SizeBytes,
    string ContentHash);

public sealed record StorageHints(
    string SuggestedFolder,
    string ContentType,
    string FileName);

/// <summary>
/// Selecciona el adapter activo (para uploads) y resuelve adapter por nombre
/// (para reads de fotos creadas con un adapter distinto del activo — RN-12).
/// </summary>
public interface IPhotoStorageAdapterFactory
{
    /// <summary>Adapter activo según SystemConfig.</summary>
    IPhotoStorageAdapter GetActive();

    /// <summary>Adapter por nombre lógico (puede no ser el activo). Útil para leer fotos viejas.</summary>
    IPhotoStorageAdapter GetByName(string adapterName);
}
