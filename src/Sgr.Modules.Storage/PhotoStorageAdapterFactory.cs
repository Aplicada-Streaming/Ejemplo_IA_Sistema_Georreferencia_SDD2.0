using Microsoft.Extensions.DependencyInjection;
using Sgr.Domain.Photos;
using Sgr.Modules.Storage.Configuration;

namespace Sgr.Modules.Storage;

/// <summary>
/// Factory de adapters de storage con hot-swap (DT-S8.1 / Sprint 11).
///
/// Para cada <see cref="GetActive"/>:
/// 1. Lee la config persistida en <c>SystemConfig.storage.active</c> (con cache TTL corto).
/// 2. Si hay config en DB → construye un adapter ephemeral con esa config.
/// 3. Si no hay config en DB → fallback al nombre de adapter de <c>Storage:ActiveAdapter</c>
///    (appsettings) y resuelve por DI un adapter pre-registrado.
///
/// Para <see cref="GetByName"/> (RN-12 reads de fotos legacy): siempre resuelve por DI.
/// El cache TTL hace que un cambio via wizard tarde a lo sumo ~30s en reflejarse — el admin
/// recibe el cambio sin reiniciar el backend.
/// </summary>
public sealed class PhotoStorageAdapterFactory : IPhotoStorageAdapterFactory
{
    private static readonly TimeSpan CacheTtl = TimeSpan.FromSeconds(30);

    private readonly IServiceProvider _services;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IStorageAdapterBuilder _builder;
    private readonly IActiveAdapterResolver _fallbackResolver;

    private readonly object _gate = new();
    private (StorageConfigDto Config, DateTime FetchedAtUtc, IPhotoStorageAdapter Adapter)? _cached;

    public PhotoStorageAdapterFactory(
        IServiceProvider services,
        IServiceScopeFactory scopeFactory,
        IStorageAdapterBuilder builder,
        IActiveAdapterResolver fallbackResolver)
    {
        _services = services;
        _scopeFactory = scopeFactory;
        _builder = builder;
        _fallbackResolver = fallbackResolver;
    }

    public IPhotoStorageAdapter GetActive()
    {
        var fromDb = ResolveFromDbWithCache();
        if (fromDb is not null) return fromDb;
        // Fallback: no hay config en DB → usamos el adapter pre-registrado de appsettings.
        return GetByName(_fallbackResolver.GetActiveAdapterName());
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

    private IPhotoStorageAdapter? ResolveFromDbWithCache()
    {
        // Fast path bajo lock corto: si hay cache fresco lo usamos.
        lock (_gate)
        {
            if (_cached is { } c && DateTime.UtcNow - c.FetchedAtUtc < CacheTtl)
                return c.Adapter;
        }

        // Slow path: leemos la config del DB. Hacemos el query fuera del lock para no
        // bloquear lectores mientras hay I/O.
        StorageConfigDto? freshConfig;
        try
        {
            using var scope = _scopeFactory.CreateScope();
            var cfgService = scope.ServiceProvider.GetRequiredService<IStorageConfigService>();
            freshConfig = cfgService.GetActiveAsync().GetAwaiter().GetResult();
        }
        catch
        {
            // Si la DB todavía no está accesible (boot, transitorios), caemos al fallback.
            return null;
        }

        if (freshConfig is null) return null;

        lock (_gate)
        {
            // Si otro thread llenó el cache mientras esperábamos, reusamos.
            if (_cached is { } c2 && DateTime.UtcNow - c2.FetchedAtUtc < CacheTtl)
                return c2.Adapter;

            var adapter = _builder.Build(freshConfig);
            _cached = (freshConfig, DateTime.UtcNow, adapter);
            return adapter;
        }
    }
}

/// <summary>
/// Resuelve el nombre del adapter activo. Hoy se usa como <b>fallback</b> cuando no hay
/// config en DB; <see cref="PhotoStorageAdapterFactory"/> lee la DB primero.
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
