using System.Text.Json;
using System.Text.Json.Serialization;

namespace Sgr.Modules.Storage.Configuration;

/// <summary>
/// Lee y persiste la configuración de storage activa (US-17 / CU-02). El servicio
/// cifra las credenciales sensibles antes de guardarlas con DataProtection y las
/// descifra al leer. Para test de conexión <see cref="ITestAsync"/> arma un adapter
/// efímero con la config dada y prueba escritura/lectura/borrado de un blob dummy.
/// </summary>
public interface IStorageConfigService
{
    Task<StorageConfigDto?> GetActiveAsync(CancellationToken ct = default);
    Task SaveAsync(StorageConfigDto config, Guid actorUserId, CancellationToken ct = default);
    Task<StorageTestResult> TestAsync(StorageConfigDto config, CancellationToken ct = default);
}

public sealed record StorageConfigDto(
    [property: JsonPropertyName("adapter")] string Adapter,
    [property: JsonPropertyName("config")] StorageAdapterConfig Config);

/// <summary>
/// Discriminated union — sólo uno de los hijos es no-null según <c>Adapter</c>.
/// Mantiene la API simple del wizard: el frontend manda el subobjeto del adapter
/// que está configurando y deja los demás en null.
/// </summary>
public sealed record StorageAdapterConfig(
    [property: JsonPropertyName("local")] LocalConfig? Local = null,
    [property: JsonPropertyName("s3")] S3Config? S3 = null,
    [property: JsonPropertyName("ftp")] FtpConfig? Ftp = null,
    [property: JsonPropertyName("sftp")] SftpConfig? Sftp = null);

public sealed record LocalConfig(string RootPath);

public sealed record S3Config(
    string BucketName,
    string Region,
    string AccessKey,
    string SecretKey,
    string? ServiceUrl,
    bool ForcePathStyle);

public sealed record FtpConfig(
    string Host,
    int Port,
    string Username,
    string Password,
    string RemoteRoot,
    bool UseTls);

public sealed record SftpConfig(
    string Host,
    int Port,
    string Username,
    string? Password,
    string? PrivateKeyPath,
    string? PrivateKeyPassphrase,
    string RemoteRoot);

public sealed record StorageTestResult(
    bool Success,
    string Message,
    long? RoundTripMs = null);

internal static class StorageConfigJson
{
    public static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web)
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };
}
