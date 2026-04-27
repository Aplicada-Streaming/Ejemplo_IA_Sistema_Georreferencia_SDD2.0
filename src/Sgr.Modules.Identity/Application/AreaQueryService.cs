using Microsoft.EntityFrameworkCore;
using Sgr.Persistence;

namespace Sgr.Modules.Identity.Application;

/// <summary>
/// Listado de áreas — necesario para el dropdown del registro y la pantalla
/// de gestión de usuarios. Endpoint público (no auth) porque se usa en el
/// formulario de registro previo al login.
/// </summary>
public interface IAreaQueryService
{
    Task<IReadOnlyList<AreaDto>> ListAsync(CancellationToken ct = default);
}

public sealed record AreaDto(Guid Id, string Name, string? Description);

public sealed class AreaQueryService : IAreaQueryService
{
    private readonly SgrDbContext _db;

    public AreaQueryService(SgrDbContext db) => _db = db;

    public async Task<IReadOnlyList<AreaDto>> ListAsync(CancellationToken ct = default) =>
        await _db.Areas.AsNoTracking()
            .OrderBy(a => a.Name)
            .Select(a => new AreaDto(a.Id, a.Name, a.Description))
            .ToListAsync(ct);
}
