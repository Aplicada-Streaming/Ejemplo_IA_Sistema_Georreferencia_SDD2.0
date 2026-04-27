namespace Sgr.Frontend.Mobile.Auth;

public interface IMobileTokenStore
{
    Task<StoredSession?> GetAsync();
    Task SetAsync(StoredSession session);
    Task ClearAsync();
}

public sealed record StoredSession(
    string AccessToken,
    DateTime ExpiresAtUtc,
    Guid UserId,
    string Email,
    string Role,
    Guid? AreaId);
