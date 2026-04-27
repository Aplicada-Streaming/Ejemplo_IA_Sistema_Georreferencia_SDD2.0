using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Sgr.Modules.Storage.Adapters;

namespace Sgr.Modules.Storage.Configuration;

/// <summary>
/// Construye una instancia ephemeral de <see cref="IPhotoStorageAdapter"/> con la config
/// provista. Compartido por:
/// - <see cref="StorageConfigService.TestAsync"/> (CA-17.3, valida sin persistir).
/// - <see cref="DbBackedPhotoStorageAdapterFactory"/> (DT-S8.1, hot-swap del activo
///   sin requerir restart del backend cuando el admin cambia la config via wizard).
/// </summary>
public interface IStorageAdapterBuilder
{
    IPhotoStorageAdapter Build(StorageConfigDto config);
}

public sealed class StorageAdapterBuilder : IStorageAdapterBuilder
{
    private readonly ILoggerFactory _loggerFactory;

    public StorageAdapterBuilder(ILoggerFactory loggerFactory) => _loggerFactory = loggerFactory;

    public IPhotoStorageAdapter Build(StorageConfigDto config)
    {
        if (config is null) throw new ArgumentNullException(nameof(config));

        return config.Adapter switch
        {
            "local" => new LocalFileSystemPhotoStorageAdapter(
                Options.Create(new LocalStorageOptions
                {
                    RootPath = config.Config.Local!.RootPath,
                }),
                _loggerFactory.CreateLogger<LocalFileSystemPhotoStorageAdapter>()),

            "s3" => new S3PhotoStorageAdapter(
                Options.Create(new S3StorageOptions
                {
                    BucketName = config.Config.S3!.BucketName,
                    Region = config.Config.S3.Region,
                    AccessKey = config.Config.S3.AccessKey,
                    SecretKey = config.Config.S3.SecretKey,
                    ServiceUrl = config.Config.S3.ServiceUrl,
                    ForcePathStyle = config.Config.S3.ForcePathStyle,
                }),
                _loggerFactory.CreateLogger<S3PhotoStorageAdapter>()),

            "ftp" => new FtpPhotoStorageAdapter(
                Options.Create(new FtpStorageOptions
                {
                    Host = config.Config.Ftp!.Host,
                    Port = config.Config.Ftp.Port,
                    Username = config.Config.Ftp.Username,
                    Password = config.Config.Ftp.Password,
                    RemoteRoot = config.Config.Ftp.RemoteRoot,
                    UseTls = config.Config.Ftp.UseTls,
                }),
                _loggerFactory.CreateLogger<FtpPhotoStorageAdapter>()),

            "sftp" => new SftpPhotoStorageAdapter(
                Options.Create(new SftpStorageOptions
                {
                    Host = config.Config.Sftp!.Host,
                    Port = config.Config.Sftp.Port,
                    Username = config.Config.Sftp.Username,
                    Password = config.Config.Sftp.Password,
                    PrivateKeyPath = config.Config.Sftp.PrivateKeyPath,
                    PrivateKeyPassphrase = config.Config.Sftp.PrivateKeyPassphrase,
                    RemoteRoot = config.Config.Sftp.RemoteRoot,
                }),
                _loggerFactory.CreateLogger<SftpPhotoStorageAdapter>()),

            _ => throw new ArgumentException($"Adapter '{config.Adapter}' desconocido."),
        };
    }
}
