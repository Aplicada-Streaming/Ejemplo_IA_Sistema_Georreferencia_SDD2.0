using Microsoft.EntityFrameworkCore;
using Sgr.Domain.Identity;
using Sgr.Modules.Identity.Authentication;
using Sgr.Persistence;

namespace Sgr.Modules.Identity.Application;

public interface ILoginService
{
    Task<LoginResponse> LoginAsync(LoginRequest request, CancellationToken ct = default);
}

public sealed class LoginService : ILoginService
{
    private readonly SgrDbContext _db;
    private readonly IPasswordHasher _hasher;
    private readonly IJwtTokenService _jwt;

    public LoginService(SgrDbContext db, IPasswordHasher hasher, IJwtTokenService jwt)
    {
        _db = db;
        _hasher = hasher;
        _jwt = jwt;
    }

    public async Task<LoginResponse> LoginAsync(LoginRequest request, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Password))
            throw new LoginException(
                LoginErrorCode.InvalidCredentials,
                "Email and password are required.");

        var normalizedEmail = request.Email.Trim().ToLowerInvariant();
        var user = await _db.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Email == normalizedEmail, ct);

        // Generic message regardless of which factor failed (CU-01 E1).
        if (user is null || !_hasher.Verify(request.Password, user.PasswordHash))
            throw new LoginException(
                LoginErrorCode.InvalidCredentials,
                "El usuario o la contraseña son incorrectos.");

        // Status-based gates (CU-01 E2/E3).
        switch (user.Status)
        {
            case UserStatus.PendienteAceptacion:
                throw new LoginException(
                    LoginErrorCode.PendingAcceptance,
                    "Tu cuenta está pendiente de aceptación.");
            case UserStatus.Inhabilitado:
                throw new LoginException(
                    LoginErrorCode.AccountDisabled,
                    "Tu cuenta está inhabilitada. Contactá al administrador.");
            case UserStatus.DadoDeBaja:
                throw new LoginException(
                    LoginErrorCode.AccountDropped,
                    "Esta cuenta no está disponible.");
        }

        // Mobile restriction (RN-11 + CU-01 E4).
        if (request.Client == ClientFront.Mobile && !user.CanLoginFromMobile())
            throw new LoginException(
                LoginErrorCode.MobileForbiddenForRole,
                "El acceso móvil está disponible solo para relevadores.");

        var token = _jwt.Issue(user, request.DeviceId);

        return new LoginResponse(
            AccessToken: token.Token,
            ExpiresAtUtc: token.ExpiresAtUtc,
            UserId: user.Id,
            Role: user.Role,
            AreaId: user.AreaId);
    }
}
