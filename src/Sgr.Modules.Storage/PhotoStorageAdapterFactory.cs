using Microsoft.Extensions.DependencyInjection;
using Sgr.Domain.Photos;

namespace Sgr.Modules.Storage;

public sealed class PhotoStorageAdapterFactory : IPhotoStorageAdapterFactory
{
    private readonly IServiceProvider _services;
    private readonly IActiveAdapterResolver _activeResolver;

    public PhotoStorageAdapterFactory(IServiceProvider services, IActiveAdapterResolver activeResolver)
    {
        _services = services;
        _activeResolver = activeResolver;
    }

    public IPhotoStorageAdapter GetActive()
    {
        var name = _activeResolver.GetActiveAdapterName();
        return GetByName(name);
    }

    public IPhotoStorageAdapter GetByName(string adapterName)
    {
        if (!StorageAdapterNames.IsValid(adapterName))
            throw new ArgumentException($"Unknown adapter name '{adapterName}'.", nameof(adapterName));

        var adapters = _services.GetServices<IPhotoStorageAdapter>();
        var match = adapters.FirstOrDefault(a => string.Equals(a.AdapterName, adapterName, StringComparison.OrdinalIgnoreCase))
            ?? throw new InvalidOperationException(
                $"No IPhotoStorageAdapter registered for name '{adapterName}'. " +
                $"Adapters disponibles: {string.Join(", ", adapters.Select(a => a.AdapterName))}");
        return match;
    }
}

/// <summary>
/// Resuelve cuál es el adapter activo. La implementación real lee de SystemConfig en DB
/// (módulo SystemConfig — fuera del scope de E.2 inicial). Por ahora se setea via opciones.
/// </summary>
public interface IActiveAdapterResolver
{
    string GetActiveAdapterName();
}

public sealed class StaticActiveAdapterResolver : IActiveAdapterResolver
{
    private readonly string _adapterName;
    public StaticActiveAdapterResolver(string adapterName) => _adapterName = adapterName;
    public string GetActiveAdapterName() => _adapterName;
}
