using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Sgr.Domain.Identity;
using Sgr.Modules.Sync.Application;

namespace Sgr.Backend.Api.Controllers;

/// <summary>
/// Panel de conflictos pendientes de revisión (US-19 / US-20).
/// Sólo admin y jefe — el relevador captura, no resuelve.
/// </summary>
[ApiController]
[Authorize(Roles = $"{UserRole.AdminRaiz},{UserRole.JefeArea}")]
[Route("api/v1/conflicts")]
[Produces("application/json")]
public sealed class ConflictsController : ControllerBase
{
    private readonly IConflictsService _conflicts;

    public ConflictsController(IConflictsService conflicts) => _conflicts = conflicts;

    /// <summary>
    /// Lista conflictos. Filtros: <c>surveyId</c>, <c>type</c> (lww/owner_precedence/post_close),
    /// <c>status</c> (pendiente/resuelto_revertido/resuelto_sin_cambio/all). Default: pendientes.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<ConflictDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<ConflictDto>>> List(
        [FromQuery] Guid? surveyId,
        [FromQuery] string? type,
        [FromQuery] string? status,
        CancellationToken ct)
    {
        var actor = ToActor();
        var rows = await _conflicts.ListAsync(surveyId, type, status, actor, ct);
        return Ok(rows);
    }

    /// <summary>Resuelve un conflicto. <c>action</c>: <c>KeepCurrent</c> | <c>Revert</c>.</summary>
    [HttpPost("{id:guid}/resolve")]
    [ProducesResponseType(typeof(ConflictDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ConflictDto>> Resolve(
        Guid id,
        [FromBody] ResolveConflictDto body,
        CancellationToken ct)
    {
        var actor = ToActor();
        var dto = await _conflicts.ResolveAsync(id, body.Action, actor, ct);
        return Ok(dto);
    }

    private Sgr.Modules.Surveys.Application.CurrentUserContext ToActor() =>
        CurrentUserAccessor.FromPrincipal(User);
}

public sealed class ResolveConflictDto
{
    /// <summary>"KeepCurrent" | "Revert"</summary>
    [Required, MaxLength(32)]
    public string Action { get; set; } = "KeepCurrent";
}
