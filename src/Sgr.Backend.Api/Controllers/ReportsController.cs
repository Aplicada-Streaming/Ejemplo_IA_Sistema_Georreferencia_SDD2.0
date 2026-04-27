using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Sgr.Modules.Surveys.Application;

namespace Sgr.Backend.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/v1/reports")]
[Produces("application/json")]
public sealed class ReportsController : ControllerBase
{
    private readonly IReportSummaryService _summary;

    public ReportsController(IReportSummaryService summary) => _summary = summary;

    /// <summary>
    /// KPIs filtrados por la visibilidad del usuario actual:
    /// admin ve todo, jefe sólo su área, relevador sólo lo propio.
    /// </summary>
    [HttpGet("summary")]
    [ProducesResponseType(typeof(ReportSummaryDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<ReportSummaryDto>> Summary(CancellationToken ct)
    {
        var current = CurrentUserAccessor.FromPrincipal(User);
        var result = await _summary.GetAsync(current, ct);
        return Ok(result);
    }
}
