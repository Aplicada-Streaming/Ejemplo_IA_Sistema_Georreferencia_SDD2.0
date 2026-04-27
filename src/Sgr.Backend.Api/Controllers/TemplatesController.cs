using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Sgr.Modules.Templates.Application;

namespace Sgr.Backend.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/v1/templates")]
[Produces("application/json")]
public sealed class TemplatesController : ControllerBase
{
    private readonly IListTemplatesService _list;
    private readonly IGetTemplateVersionService _get;

    public TemplatesController(IListTemplatesService list, IGetTemplateVersionService get)
    {
        _list = list;
        _get = get;
    }

    /// <summary>List all templates with their latest published version, if any.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<TemplateSummaryDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<TemplateSummaryDto>>> List(CancellationToken ct)
    {
        var result = await _list.ListAsync(ct);
        return Ok(result);
    }

    /// <summary>
    /// Get a TemplateVersion detail with its parsed schema (fields + capture params).
    /// El cliente recibe los campos como objetos tipados (no JSON crudo).
    /// </summary>
    [HttpGet("versions/{versionId:guid}")]
    [ProducesResponseType(typeof(TemplateVersionDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<TemplateVersionDetailDto>> GetVersion(Guid versionId, CancellationToken ct)
    {
        var result = await _get.GetByIdAsync(versionId, ct);
        return Ok(result);
    }
}
