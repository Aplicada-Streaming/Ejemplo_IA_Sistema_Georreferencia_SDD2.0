using Sgr.Domain.Identity;

namespace Sgr.Modules.Identity.Authentication;

public interface IJwtTokenService
{
    AccessToken Issue(User user, string? deviceId = null);
}

public sealed record AccessToken(string Token, DateTime ExpiresAtUtc);
