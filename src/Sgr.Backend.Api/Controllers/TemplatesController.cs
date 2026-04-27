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
    private readonly ITemplateEditorService _editor;

    public TemplatesController(
        IListTemplatesService list,
        IGetTemplateVersionService get,
        ITemplateEditorService editor)
    {
        _list = list;
        _get = get;
        _editor = editor;
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

    // ───────── Editor (E.5.c) — sólo admin ─────────

    /// <summary>Crea plantilla hija + v1 borrador clonada de la raíz publicada.</summary>
    [HttpPost]
    [Authorize(Roles = "admin_raiz")]
    [ProducesResponseType(typeof(TemplateCreatedDto), StatusCodes.Status201Created)]
    public async Task<ActionResult<TemplateCreatedDto>> Create([FromBody] CreateTemplateBody body, CancellationToken ct)
    {
        var result = await _editor.CreateTemplateAsync(body.Name, ct);
        return CreatedAtAction(nameof(GetVersion), new { versionId = result.DraftVersionId }, result);
    }

    /// <summary>Actualiza fields de una versión en borrador.</summary>
    [HttpPut("versions/{versionId:guid}/fields")]
    [Authorize(Roles = "admin_raiz")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> UpdateFields(
        Guid versionId,
        [FromBody] UpdateFieldsBody body,
        CancellationToken ct)
    {
        await _editor.UpdateFieldsAsync(versionId, body.FieldDefinitionsJson, ct);
        return NoContent();
    }

    /// <summary>Actualiza captureParams de una versión en borrador.</summary>
    [HttpPut("versions/{versionId:guid}/capture-params")]
    [Authorize(Roles = "admin_raiz")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> UpdateCaptureParams(
        Guid versionId,
        [FromBody] UpdateCaptureParamsBody body,
        CancellationToken ct)
    {
        await _editor.UpdateCaptureParamsAsync(versionId, body.CaptureParamsJson, ct);
        return NoContent();
    }

    /// <summary>Publica un borrador. Idempotente.</summary>
    [HttpPost("versions/{versionId:guid}/publish")]
    [Authorize(Roles = "admin_raiz")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Publish(Guid versionId, CancellationToken ct)
    {
        await _editor.PublishAsync(versionId, ct);
        return NoContent();
    }
}

public sealed record CreateTemplateBody(string Name);
public sealed record UpdateFieldsBody(string FieldDefinitionsJson);
public sealed record UpdateCaptureParamsBody(string CaptureParamsJson);
