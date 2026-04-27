using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Sgr.Domain.Identity;
using Sgr.Modules.Identity.Application;

namespace Sgr.Backend.Api.Controllers;

/// <summary>
/// Endpoints de gestión jerárquica de usuarios (US-13).
///
/// Reglas de scope (RN-11) — enforced en <see cref="UserManagementService"/>:
/// - admin_raiz puede operar sobre jefes_area.
/// - jefe_area puede operar sobre relevadores de su misma <c>area_id</c>.
/// - relevador no entra (bloqueado por <c>[Authorize(Roles)]</c>).
/// </summary>
[ApiController]
[Authorize(Roles = $"{UserRole.AdminRaiz},{UserRole.JefeArea}")]
[Route("api/v1/users")]
[Produces("application/json")]
public sealed class UsersController : ControllerBase
{
    private readonly IUserManagementService _users;

    public UsersController(IUserManagementService users) => _users = users;

    /// <summary>Lista de usuarios pendientes de aceptación, scoped por rol.</summary>
    [HttpGet("pending")]
    [ProducesResponseType(typeof(IReadOnlyList<UserDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<UserDto>>> ListPending(CancellationToken ct)
    {
        var actor = ActorFromPrincipal();
        var users = await _users.ListPendingAsync(actor, ct);
        return Ok(users);
    }

    /// <summary>Lista completa de usuarios bajo el scope del actor.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<UserDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<UserDto>>> List(CancellationToken ct)
    {
        var actor = ActorFromPrincipal();
        var users = await _users.ListAllAsync(actor, ct);
        return Ok(users);
    }

    [HttpPost("{id:guid}/accept")]
    [ProducesResponseType(typeof(UserDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<UserDto>> Accept(Guid id, CancellationToken ct)
    {
        var dto = await _users.AcceptAsync(id, ActorFromPrincipal(), ct);
        return Ok(dto);
    }

    [HttpPost("{id:guid}/reject")]
    [ProducesResponseType(typeof(UserDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<UserDto>> Reject(Guid id, CancellationToken ct)
    {
        var dto = await _users.RejectAsync(id, ActorFromPrincipal(), ct);
        return Ok(dto);
    }

    [HttpPost("{id:guid}/disable")]
    [ProducesResponseType(typeof(UserDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<UserDto>> Disable(Guid id, CancellationToken ct)
    {
        var dto = await _users.DisableAsync(id, ActorFromPrincipal(), ct);
        return Ok(dto);
    }

    [HttpPost("{id:guid}/enable")]
    [ProducesResponseType(typeof(UserDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<UserDto>> Enable(Guid id, CancellationToken ct)
    {
        var dto = await _users.EnableAsync(id, ActorFromPrincipal(), ct);
        return Ok(dto);
    }

    private UserActorContext ActorFromPrincipal()
    {
        var current = CurrentUserAccessor.FromPrincipal(User);
        return new UserActorContext(current.UserId, current.Role, current.AreaId);
    }
}
