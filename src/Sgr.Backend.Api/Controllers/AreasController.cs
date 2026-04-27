using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Sgr.Modules.Identity.Application;

namespace Sgr.Backend.Api.Controllers;

/// <summary>
/// Listado de áreas. <see cref="HttpGet"/> es público (anónimo) porque el
/// formulario de registro de la web lo necesita antes del login.
/// </summary>
[ApiController]
[Route("api/v1/areas")]
[Produces("application/json")]
public sealed class AreasController : ControllerBase
{
    private readonly IAreaQueryService _areas;

    public AreasController(IAreaQueryService areas) => _areas = areas;

    [HttpGet]
    [AllowAnonymous]
    [ProducesResponseType(typeof(IReadOnlyList<AreaDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<AreaDto>>> List(CancellationToken ct)
    {
        var areas = await _areas.ListAsync(ct);
        return Ok(areas);
    }
}
