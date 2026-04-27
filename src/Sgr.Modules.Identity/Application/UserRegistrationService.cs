using Microsoft.EntityFrameworkCore;
using Sgr.Domain.Common;
using Sgr.Domain.Identity;
using Sgr.Modules.Identity.Authentication;
using Sgr.Persistence;

namespace Sgr.Modules.Identity.Application;

/// <summary>
/// Self-registration de jefes y relevadores. El usuario queda en
/// estado <see cref="UserStatus.PendienteAceptacion"/> y debe ser
/// aceptado por su superior jerárquico (US-13 / RN-11).
///
/// Admin raíz NO se registra por aquí; se inicializa en el seed.
/// </summary>
public interface IUserRegistrationService
{
    Task<UserRegistrationResponse> RegisterAsync(
        UserRegistrationRequest request,
        CancellationToken ct = default);
}

public sealed record UserRegistrationRequest(
    string Email,
    string Password,
    string FullName,
    string Role,
    Guid? AreaId);

public sealed record UserRegistrationResponse(
    Guid UserId,
    string Status);

public sealed class UserRegistrationService : IUserRegistrationService
{
    private readonly SgrDbContext _db;
    private readonly IPasswordHasher _hasher;
    private readonly IDateTimeProvider _clock;

    public UserRegistrationService(
        SgrDbContext db,
        IPasswordHasher hasher,
        IDateTimeProvider clock)
    {
        _db = db;
        _hasher = hasher;
        _clock = clock;
    }

    public async Task<UserRegistrationResponse> RegisterAsync(
        UserRegistrationRequest request,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (string.IsNullOrWhiteSpace(request.Password) || request.Password.Length < 8)
            throw new UserManagementException(
                UserManagementErrorCode.InvalidInput,
                "La contraseña debe tener al menos 8 caracteres.");

        if (!UserRole.IsValid(request.Role) || request.Role == UserRole.AdminRaiz)
            throw new UserManagementException(
                UserManagementErrorCode.InvalidInput,
                "Rol inválido para auto-registro.");

        if (request.AreaId is null)
            throw new UserManagementException(
                UserManagementErrorCode.InvalidInput,
                "Es obligatorio seleccionar un área.");

        // El area debe existir — sino el FK fallaría más adelante con un mensaje pobre.
        var areaExists = await _db.Areas.AnyAsync(a => a.Id == request.AreaId.Value, ct);
        if (!areaExists)
            throw new UserManagementException(
                UserManagementErrorCode.AreaNotFound,
                "El área seleccionada no existe.");

        var normalizedEmail = request.Email.Trim().ToLowerInvariant();
        var emailTaken = await _db.Users.AnyAsync(u => u.Email == normalizedEmail, ct);
        if (emailTaken)
            throw new UserManagementException(
                UserManagementErrorCode.EmailTaken,
                "Ya existe un usuario con ese email.");

        var user = User.RegisterPending(
            id: Guid.NewGuid(),
            email: request.Email,
            passwordHash: _hasher.Hash(request.Password),
            fullName: request.FullName,
            role: request.Role,
            areaId: request.AreaId,
            createdAt: _clock.UtcNow);

        _db.Users.Add(user);
        await _db.SaveChangesAsync(ct);

        return new UserRegistrationResponse(user.Id, user.Status);
    }
}
