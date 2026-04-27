using System.Text.Json;

namespace Sgr.Frontend.Mobile.Auth;

/// <summary>
/// Persists the session in MAUI SecureStorage when available; falls back to in-memory.
/// In-memory fallback covers Windows targets where SecureStorage is not always available
/// without additional configuration. Production builds for Android/iOS use SecureStorage.
/// </summary>
public sealed class SecureMobileTokenStore : IMobileTokenStore
{
    private const string Key = "sgr.session";
    private StoredSession? _cache;

    public async Task<StoredSession?> GetAsync()
    {
        if (_cache is not null) return _cache;

        try
        {
            var json = await SecureStorage.Default.GetAsync(Key);
            if (string.IsNullOrEmpty(json)) return null;

            _cache = JsonSerializer.Deserialize<StoredSession>(json);
            if (_cache is not null && _cache.ExpiresAtUtc <= DateTime.UtcNow)
            {
                await ClearAsync();
                return null;
            }
            return _cache;
        }
        catch
        {
            // SecureStorage not supported on this target — keep in-memory only.
            return _cache;
        }
    }

    public async Task SetAsync(StoredSession session)
    {
        _cache = session;
        try
        {
            var json = JsonSerializer.Serialize(session);
            await SecureStorage.Default.SetAsync(Key, json);
        }
        catch
        {
            // ignore — in-memory cache is still populated
        }
    }

    public async Task ClearAsync()
    {
        _cache = null;
        try
        {
            SecureStorage.Default.Remove(Key);
            await Task.CompletedTask;
        }
        catch
        {
            // ignore
        }
    }
}
