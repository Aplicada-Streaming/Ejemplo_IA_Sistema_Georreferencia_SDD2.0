using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Sgr.Persistence;

namespace Sgr.Backend.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/v1/points")]
public sealed class PointsController : ControllerBase
{
    private readonly SgrDbContext _db;

    public PointsController(SgrDbContext db) => _db = db;

    /// <summary>
    /// Lista los valores de campos de plantilla persistidos para el punto (E.5.b).
    /// El cliente conoce el tipo de cada campo consultando la VersiónDePlantilla
    /// del relevamiento al que pertenece el punto.
    /// </summary>
    [HttpGet("{pointId:guid}/field-values")]
    [ProducesResponseType(typeof(IReadOnlyList<PointFieldValueDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<PointFieldValueDto>>> ListFieldValues(
        Guid pointId, CancellationToken ct)
    {
        var values = await _db.PointFieldValues.AsNoTracking()
            .Where(v => v.PointId == pointId)
            .OrderBy(v => v.FieldKey)
            .Select(v => new PointFieldValueDto(
                v.PointId, v.FieldKey, v.ValueJson, v.UpdatedAt, v.UpdatedBy))
            .ToListAsync(ct);
        return Ok(values);
    }
}

public sealed record PointFieldValueDto(
    Guid PointId,
    string FieldKey,
    string? ValueJson,
    DateTime UpdatedAt,
    Guid UpdatedBy);
