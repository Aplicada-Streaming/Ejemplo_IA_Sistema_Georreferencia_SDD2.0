using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Sgr.Domain.Photos;
using Sgr.Modules.Storage.Adapters;
using Sgr.Modules.Storage.Configuration;
using Sgr.Modules.Storage.Imaging;

namespace Sgr.Modules.Storage;

public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registra el módulo Storage con todos los adapters disponibles.
    /// El adapter activo se determina por <c>Storage:ActiveAdapter</c> en la configuración.
    /// </summary>
    public static IServiceCollection AddSgrStorage(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.Configure<LocalStorageOptions>(configuration.GetSection(LocalStorageOptions.SectionName));
        services.Configure<S3StorageOptions>(configuration.GetSection(S3StorageOptions.SectionName));
        services.Configure<FtpStorageOptions>(configuration.GetSection(FtpStorageOptions.SectionName));
        services.Configure<SftpStorageOptions>(configuration.GetSection(SftpStorageOptions.SectionName));

        // Adapters registrados como multi-binding bajo IPhotoStorageAdapter.
        // El factory selecciona el activo y permite leer fotos de adapters legacy (RN-12).
        services.AddSingleton<IPhotoStorageAdapter, LocalFileSystemPhotoStorageAdapter>();
        services.AddSingleton<IPhotoStorageAdapter, S3PhotoStorageAdapter>();
        services.AddSingleton<IPhotoStorageAdapter, FtpPhotoStorageAdapter>();
        services.AddSingleton<IPhotoStorageAdapter, SftpPhotoStorageAdapter>();

        var activeAdapter = configuration["Storage:ActiveAdapter"] ?? StorageAdapterNames.Local;
        services.AddSingleton<IActiveAdapterResolver>(new StaticActiveAdapterResolver(activeAdapter));
        services.AddSingleton<IPhotoStorageAdapterFactory, PhotoStorageAdapterFactory>();

        // Slice 7 / US-15 — lectura de EXIF para subida en lote desde web.
        services.AddSingleton<IExifReader, MetadataExtractorExifReader>();

        // Slice 8 / US-17 — config de storage persistida con cifrado de credenciales.
        services.AddDataProtection();
        services.AddScoped<IStorageConfigService, StorageConfigService>();

        return services;
    }
}
