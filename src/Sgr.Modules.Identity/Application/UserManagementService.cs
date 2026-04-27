using Microsoft.EntityFrameworkCore;
using Sgr.Domain.Common;
using Sgr.Domain.Identity;
using Sgr.Persistence;

namespace Sgr.Modules.Identity.Application;

/// <summary>
/// Aceptación / inhabilitación / rechazo de usuarios — US-13 / RN-11.
///
/// **Regla de scope (CA-13.5):**
/// - admin_raiz puede operar sobre cualquier jefe_area.
/// - jefe_area sólo puede operar sobre relevadores de **su misma** AreaId.
/// - Cualquier otra combinación → <see cref="UserManagementErrorCode.Forbidden"/>.
///
/// El servicio acepta un <see cref="UserActorContext"/> con la info del que
/// invoca (extraída del JWT en el controller). Mantenerlo afuera del HTTP
/// permite testear las reglas sin levantar la app.
/// </summary>
public interface IUserManagementService
{
    Task<IReadOnlyList<UserDto>> ListPendingAsync(UserActorContext actor, CancellationToken ct = default);
    Task<IReadOnlyList<UserDto>> ListAllAsync(UserActorContext actor, CancellationToken ct = default);
    Task<UserDto> AcceptAsync(Guid userId, UserActorContext actor, CancellationToken ct = default);
    Task<UserDto> RejectAsync(Guid userId, UserActorContext actor, CancellationToken ct = default);
    Task<UserDto> DisableAsync(Guid userId, UserActorContext actor, CancellationToken ct = default);
    Task<UserDto> EnableAsync(Guid userId, UserActorContext actor, CancellationToken ct = default);
}

public sealed record UserActorContext(Guid UserId, string Role, Guid? AreaId);

public sealed record UserDto(
    Guid Id,
    string Email,
    string FullName,
    string Role,
    string Status,
    Guid? AreaId,
    DateTime? AcceptedAt,
    DateTime CreatedAt);

public sealed class UserManagementService : IUserManagementService
{
    private readonly SgrDbContext _db;
    private readonly IDateTimeProvider _clock;

    public UserManagementService(SgrDbContext db, IDateTimeProvider clock)
    {
        _db = db;
        _clock = clock;
    }

    public async Task<IReadOnlyList<UserDto>> ListPendingAsync(
        UserActorContext actor,
        CancellationToken ct = default)
    {
        var query = _db.Users.AsNoTracking()
            .Where(u => u.Status == UserStatus.PendienteAceptacion);

        query = ScopeListByActor(query, actor);

        var users = await query.OrderBy(u => u.CreatedAt).ToListAsync(ct);
        return users.Select(ToDto).ToList();
    }

    public async Task<IReadOnlyList<UserDto>> ListAllAsync(
        UserActorContext actor,
        CancellationToken ct = default)
    {
        var query = _db.Users.AsNoTracking();
        query = ScopeListByActor(query, actor);
        var users = await query.OrderBy(u => u.FullName).ToListAsync(ct);
        return users.Select(ToDto).ToList();
    }

    public async Task<UserDto> AcceptAsync(Guid userId, UserActorContext actor, CancellationToken ct = default)
    {
        var user = await LoadAndAuthorizeAsync(userId, actor, ct);
        try { user.Accept(_clock.UtcNow); }
        catch (InvalidOperationException ex)
        {
            throw new UserManagementException(UserManagementErrorCode.InvalidStateTransition, ex.Message);
        }
        await _db.SaveChangesAsync(ct);
        return ToDto(user);
    }

    public async Task<UserDto> RejectAsync(Guid userId, UserActorContext actor, CancellationToken ct = default)
    {
        var user = await LoadAndAuthorizeAsync(userId, actor, ct);
        // Rechazo = dado_de_baja. Coincide con el verbo "delete" del doc de US-13
        // (DropOff es soft-delete; no se borra físicamente para mantener auditoría).
        user.DropOff();
        await _db.SaveChangesAsync(ct);
        return ToDto(user);
    }

    public async Task<UserDto> DisableAsync(Guid userId, UserActorContext actor, CancellationToken ct = default)
    {
        var user = await LoadAndAuthorizeAsync(userId, actor, ct);
        try { user.Disable(); }
        catch (InvalidOperationException ex)
        {
            throw new UserManagementException(UserManagementErrorCode.InvalidStateTransition, ex.Message);
        }
        await _db.SaveChangesAsync(ct);
        return ToDto(user);
    }

    public async Task<UserDto> EnableAsync(Guid userId, UserActorContext actor, CancellationToken ct = default)
    {
        var user = await LoadAndAuthorizeAsync(userId, actor, ct);
        try { user.Enable(); }
        catch (InvalidOperationException ex)
        {
            throw new UserManagementException(UserManagementErrorCode.InvalidStateTransition, ex.Message);
        }
        await _db.SaveChangesAsync(ct);
        return ToDto(user);
    }

    private async Task<User> LoadAndAuthorizeAsync(
        Guid userId,
        UserActorContext actor,
        CancellationToken ct)
    {
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == userId, ct)
            ?? throw new UserManagementException(
                UserManagementErrorCode.UserNotFound,
                "Usuario no encontrado.");

        if (!CanActOn(actor, user))
            throw new UserManagementException(
                UserManagementErrorCode.Forbidden,
                "No tenés permiso para operar sobre este usuario.");

        return user;
    }

    private static IQueryable<User> ScopeListByActor(IQueryable<User> query, UserActorContext actor)
    {
        // admin_raiz ve a todos los jefes (no se ve a sí mismo en el listado).
        // jefe_area ve a los relevadores de su misma área.
        // relevador no entra acá (lo bloquea el authorize del controller con [Authorize(Roles=...)]).
        return actor.Role switch
        {
            UserRole.AdminRaiz => query.Where(u => u.Role == UserRole.JefeArea),
            UserRole.JefeArea => query.Where(u =>
                u.Role == UserRole.Relevador && u.AreaId == actor.AreaId),
            _ => query.Where(_ => false),
        };
    }

    private static bool CanActOn(UserActorContext actor, User target) => actor.Role switch
    {
        UserRole.AdminRaiz => target.Role == UserRole.JefeArea,
        UserRole.JefeArea => target.Role == UserRole.Relevador && target.AreaId == actor.AreaId,
        _ => false,
    };

    private static UserDto ToDto(User u) => new(
        u.Id, u.Email, u.FullName, u.Role, u.Status, u.AreaId, u.AcceptedAt, u.CreatedAt);
}
