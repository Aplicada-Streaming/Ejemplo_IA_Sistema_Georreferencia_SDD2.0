using System.Security.Claims;
using Microsoft.AspNetCore.Components.Authorization;

namespace Sgr.Frontend.Mobile.Auth;

public sealed class MobileAuthenticationStateProvider : AuthenticationStateProvider
{
    private readonly IMobileTokenStore _store;
    private static readonly AuthenticationState Anonymous = new(new ClaimsPrincipal(new ClaimsIdentity()));

    public MobileAuthenticationStateProvider(IMobileTokenStore store) => _store = store;

    public override async Task<AuthenticationState> GetAuthenticationStateAsync()
    {
        var session = await _store.GetAsync();
        if (session is null) return Anonymous;

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, session.UserId.ToString()),
            new(ClaimTypes.Email, session.Email),
            new(ClaimTypes.Name, session.Email),
            new(ClaimTypes.Role, session.Role),
        };
        if (session.AreaId is not null)
            claims.Add(new Claim("area_id", session.AreaId.Value.ToString()));

        var identity = new ClaimsIdentity(claims, authenticationType: "mobile-jwt");
        return new AuthenticationState(new ClaimsPrincipal(identity));
    }

    public async Task SignInAsync(StoredSession session)
    {
        await _store.SetAsync(session);
        NotifyAuthenticationStateChanged(GetAuthenticationStateAsync());
    }

    public async Task SignOutAsync()
    {
        await _store.ClearAsync();
        NotifyAuthenticationStateChanged(Task.FromResult(Anonymous));
    }
}
