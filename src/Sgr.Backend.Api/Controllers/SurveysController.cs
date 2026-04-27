using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Sgr.Backend.Api.Startup;
using Sgr.Domain.Audit;
using Sgr.Modules.Surveys.Application;
using Sgr.Modules.Templates.Application;

namespace Sgr.Backend.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/v1/surveys")]
[Produces("application/json")]
public sealed class SurveysController : ControllerBase
{
    private readonly ICreateSurveyService _create;
    private readonly IListSurveysService _list;
    private readonly IListSurveyPointsService _listPoints;
    private readonly IGetSurveyService _get;
    private readonly ICloseSurveyService _close;
    private readonly IGetTemplateVersionService _getTemplate;
    private readonly IExportSurveyService _export;
    private readonly SurveyZipBundler _zipBundler;

    public SurveysController(
        ICreateSurveyService create,
        IListSurveysService list,
        IListSurveyPointsService listPoints,
        IGetSurveyService get,
        ICloseSurveyService close,
        IGetTemplateVersionService getTemplate,
        IExportSurveyService export,
        SurveyZipBundler zipBundler)
    {
        _create = create;
        _list = list;
        _listPoints = listPoints;
        _get = get;
        _close = close;
        _getTemplate = getTemplate;
        _export = export;
        _zipBundler = zipBundler;
    }

    /// <summary>List surveys visible to the current user.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<SurveyDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<SurveyDto>>> List(
        [FromQuery] string? status,
        CancellationToken ct)
    {
        var current = CurrentUserAccessor.FromPrincipal(User);
        var result = await _list.ListAsync(current, status, ct);
        return Ok(result);
    }

    /// <summary>Create a new survey. Idempotent by client-generated GUID (RN-06).</summary>
    [HttpPost]
    [ProducesResponseType(typeof(SurveyDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<SurveyDto>> Create(
        [FromBody] CreateSurveyDto body,
        CancellationToken ct)
    {
        var current = CurrentUserAccessor.FromPrincipal(User);

        var origin = body.Origin ?? AuditOrigin.WebEdit;
        var request = new CreateSurveyRequest(
            SurveyId: body.Id,
            Name: body.Name,
            Description: body.Description,
            TemplateVersionId: body.TemplateVersionId,
            Tags: body.Tags,
            Origin: origin,
            DeviceId: body.DeviceId,
            TimestampOriginal: body.TimestampOriginal ?? DateTime.UtcNow);

        var result = await _create.CreateAsync(request, current, ct);
        return CreatedAtAction(nameof(List), new { }, result);
    }

    /// <summary>List points (no borrados) of a survey, ordered by CreatedAt ASC.
    /// Visibilidad por rol — admin todo, jefe por área, relevador propio (US-09 cierra el filtrado completo).</summary>
    [HttpGet("{id:guid}/points")]
    [ProducesResponseType(typeof(IReadOnlyList<PointDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IReadOnlyList<PointDto>>> ListPoints(
        Guid id,
        CancellationToken ct)
    {
        var current = CurrentUserAccessor.FromPrincipal(User);
        var result = await _listPoints.ListAsync(id, current, ct);
        return Ok(result);
    }

    /// <summary>Get survey detail by id, with the same visibility rules as List.</summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(SurveyDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<SurveyDto>> Get(Guid id, CancellationToken ct)
    {
        var current = CurrentUserAccessor.FromPrincipal(User);
        var result = await _get.GetByIdAsync(id, current, ct);
        return Ok(result);
    }

    /// <summary>Close a survey. Sólo admin o jefe del área. Idempotente.</summary>
    [HttpPost("{id:guid}/close")]
    [ProducesResponseType(typeof(SurveyDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<SurveyDto>> Close(Guid id, CancellationToken ct)
    {
        var current = CurrentUserAccessor.FromPrincipal(User);
        var result = await _close.CloseAsync(id, current, ct);
        return Ok(result);
    }

    /// <summary>
    /// Exporta el relevamiento en CSV o GeoJSON. <c>format</c> = csv | geojson.
    /// El CSV trae BOM UTF-8 para que Excel lo abra con acentos OK.
    /// </summary>
    [HttpGet("{id:guid}/export")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Export(
        Guid id,
        [FromQuery] string format,
        CancellationToken ct)
    {
        var current = CurrentUserAccessor.FromPrincipal(User);
        var fmt = (format ?? "csv").Trim().ToLowerInvariant();

        // ZIP streaming directo a Response.Body para no buffearlo en memoria
        // (DT-export-zip-streaming). Los otros formatos siguen como byte[]
        // porque son chicos (KBs).
        if (fmt == "zip")
        {
            // ZipArchive.Dispose escribe el central directory con I/O síncrono;
            // Kestrel bloquea eso por default. Habilitamos sync I/O sólo para
            // esta request — la alternativa es bufferear todo el ZIP en memoria
            // (lo que justamente queremos evitar).
            var syncIo = HttpContext.Features.Get<Microsoft.AspNetCore.Http.Features.IHttpBodyControlFeature>();
            if (syncIo is not null) syncIo.AllowSynchronousIO = true;

            Response.ContentType = "application/zip";
            Response.Headers.ContentDisposition = $"attachment; filename=\"survey-{id}.zip\"";
            await _zipBundler.BuildAsync(Response.Body, id, current, ct);
            return new EmptyResult();
        }

        return fmt switch
        {
            "csv" => File(await _export.GenerateCsvAsync(id, current, ct),
                "text/csv; charset=utf-8", $"survey-{id}.csv"),
            "xlsx" => File(await _export.GenerateXlsxAsync(id, current, ct),
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                $"survey-{id}.xlsx"),
            "geojson" => File(await _export.GenerateGeoJsonAsync(id, current, ct),
                "application/geo+json", $"survey-{id}.geojson"),
            _ => BadRequest(new ProblemDetails
            {
                Status = 400,
                Title = "Bad Request",
                Detail = $"Formato '{format}' no soportado. Usar csv | xlsx | geojson | zip.",
            }),
        };
    }

    /// <summary>
    /// Devuelve el detalle de la TemplateVersion asignada al relevamiento.
    /// El cliente móvil usa este endpoint para configurar la captura
    /// (timeouts, threshold de precisión, radio de descarte) en lugar de
    /// hardcoded constants — Slice 5 / E.5.
    /// </summary>
    [HttpGet("{id:guid}/template-version")]
    [ProducesResponseType(typeof(TemplateVersionDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<TemplateVersionDetailDto>> GetTemplateVersion(Guid id, CancellationToken ct)
    {
        // Reusamos GetSurveyAsync para ejercitar las reglas de visibilidad por rol
        // antes de devolver el schema. Si el usuario no ve el survey, tampoco ve la plantilla.
        var current = CurrentUserAccessor.FromPrincipal(User);
        await _get.GetByIdAsync(id, current, ct);

        var result = await _getTemplate.GetForSurveyAsync(id, ct);
        return Ok(result);
    }
}

public sealed class CreateSurveyDto
{
    /// <summary>GUID generated by the client (RN-06).</summary>
    [Required]
    public Guid Id { get; set; }

    [Required, MaxLength(200)]
    public string Name { get; set; } = default!;

    [MaxLength(1000)]
    public string? Description { get; set; }

    /// <summary>If null, the root template (latest published version) is used.</summary>
    public Guid? TemplateVersionId { get; set; }

    [MaxLength(500)]
    public string? Tags { get; set; }

    /// <summary>web_edit | mobile_capture | mobile_edit | web_manual_upload</summary>
    [MaxLength(32)]
    public string? Origin { get; set; }

    [MaxLength(64)]
    public string? DeviceId { get; set; }

    public DateTime? TimestampOriginal { get; set; }
}
