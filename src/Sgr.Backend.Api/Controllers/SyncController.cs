using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Sgr.Modules.Sync.Application;

namespace Sgr.Backend.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/v1/sync")]
[Produces("application/json")]
public sealed class SyncController : ControllerBase
{
    private readonly IEventApplier _applier;

    public SyncController(IEventApplier applier) => _applier = applier;

    /// <summary>
    /// Push de eventos al backend (US-04).
    /// Idempotente por <c>event_id</c> (RN-06).
    /// Resuelve LWW por campo + precedencia del dueño (RN-07).
    /// Rechaza capturas posteriores al cierre del survey (RN-08).
    /// </summary>
    [HttpPost("push")]
    [ProducesResponseType(typeof(SyncPushResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<SyncPushResponse>> Push(
        [FromBody] PushRequestDto body,
        CancellationToken ct)
    {
        if (body?.Events is null || body.Events.Count == 0)
            return BadRequest(new ProblemDetails
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "Bad Request",
                Detail = "events array is required and must contain at least one event.",
            });

        var response = await _applier.ApplyAsync(body.Events, ct);
        return Ok(response);
    }
}

public sealed class PushRequestDto
{
    public List<SyncEventDto> Events { get; set; } = new();
}

[ApiController]
[Authorize]
[Route("api/v1/surveys")]
[Produces("application/json")]
public sealed class SurveysSyncController : ControllerBase
{
    private readonly IPullDiffService _pull;

    public SurveysSyncController(IPullDiffService pull) => _pull = pull;

    /// <summary>
    /// Pull diferencial (US-05): devuelve los eventos del survey posteriores a <c>since</c>.
    /// El cliente persiste el <c>NextSince</c> del response y lo envía en el próximo pull.
    /// </summary>
    [HttpGet("{surveyId:guid}/changes")]
    [ProducesResponseType(typeof(PullDiffResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PullDiffResponse>> Changes(
        Guid surveyId,
        [FromQuery] DateTime? since,
        [FromQuery(Name = "max")] int maxBatchSize,
        CancellationToken ct)
    {
        if (maxBatchSize <= 0) maxBatchSize = 200;
        try
        {
            var response = await _pull.PullAsync(surveyId, since, maxBatchSize, ct);
            return Ok(response);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new ProblemDetails
            {
                Status = StatusCodes.Status404NotFound,
                Title = "Survey not found",
                Detail = ex.Message,
            });
        }
    }
}
