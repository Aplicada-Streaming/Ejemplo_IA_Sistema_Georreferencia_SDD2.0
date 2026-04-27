using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Sgr.Domain.Identity;
using Sgr.Modules.Sync.Application;

namespace Sgr.Backend.Api.Controllers;

/// <summary>Candidatos a fusión (US-21 / US-22 / CU-11).</summary>
[ApiController]
[Authorize(Roles = $"{UserRole.AdminRaiz},{UserRole.JefeArea}")]
[Route("api/v1/merge-candidates")]
[Produces("application/json")]
public sealed class MergeCandidatesController : ControllerBase
{
    private readonly IMergeCandidatesService _candidates;

    public MergeCandidatesController(IMergeCandidatesService candidates) => _candidates = candidates;

    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<MergeCandidateDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<MergeCandidateDto>>> List(
        [FromQuery] Guid? surveyId,
        [FromQuery] string? status,
        CancellationToken ct)
    {
        var actor = CurrentUserAccessor.FromPrincipal(User);
        var rows = await _candidates.ListAsync(surveyId, status, actor, ct);
        return Ok(rows);
    }

    /// <summary>Marca el par como <c>mantenido_separado</c> — RN-09 no lo vuelve a proponer.</summary>
    [HttpPost("{id:guid}/keep-separate")]
    [ProducesResponseType(typeof(MergeCandidateDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<MergeCandidateDto>> KeepSeparate(Guid id, CancellationToken ct)
    {
        var actor = CurrentUserAccessor.FromPrincipal(User);
        var dto = await _candidates.KeepSeparateAsync(id, actor, ct);
        return Ok(dto);
    }

    /// <summary>
    /// Fusiona el par. <c>strategy</c>: <c>centroid</c> | <c>keep_a</c> | <c>keep_b</c>.
    /// El kept conserva sus datos; las fotos del dropped se le mueven; el dropped queda
    /// soft-deleted; se registra AuditEvent <c>merged</c>.
    /// </summary>
    [HttpPost("{id:guid}/merge")]
    [ProducesResponseType(typeof(MergeCandidateDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<MergeCandidateDto>> Merge(
        Guid id,
        [FromBody] MergeRequestDto body,
        CancellationToken ct)
    {
        var actor = CurrentUserAccessor.FromPrincipal(User);
        var dto = await _candidates.MergeAsync(id, body.Strategy, actor, ct);
        return Ok(dto);
    }
}

public sealed class MergeRequestDto
{
    /// <summary>"centroid" | "keep_a" | "keep_b"</summary>
    [Required, MaxLength(16)]
    public string Strategy { get; set; } = "centroid";
}
