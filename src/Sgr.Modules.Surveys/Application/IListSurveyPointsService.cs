using Microsoft.EntityFrameworkCore;
using Sgr.Domain.Identity;
using Sgr.Persistence;

namespace Sgr.Modules.Surveys.Application;

public interface IListSurveyPointsService
{
    /// <summary>
    /// Lista los puntos no borrados del relevamiento. La visibilidad sigue las
    /// mismas reglas que <see cref="IListSurveysService"/> en este sprint:
    /// admin ve todo, jefe ve los de su área, relevador sólo los suyos.
    /// </summary>
    Task<IReadOnlyList<PointDto>> ListAsync(
        Guid surveyId,
        CurrentUserContext currentUser,
        CancellationToken ct = default);
}

public sealed class ListSurveyPointsService : IListSurveyPointsService
{
    private readonly SgrDbContext _db;

    public ListSurveyPointsService(SgrDbContext db) => _db = db;

    public async Task<IReadOnlyList<PointDto>> ListAsync(
        Guid surveyId,
        CurrentUserContext currentUser,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(currentUser);

        var survey = await _db.Surveys.AsNoTracking()
            .Where(s => s.Id == surveyId && !s.IsDeleted)
            .Select(s => new { s.Id, s.AreaId, s.OwnerId })
            .FirstOrDefaultAsync(ct);
        if (survey is null)
            throw new SurveyException(SurveyErrorCode.NotFound, $"Survey {surveyId} no existe.");

        var visible = currentUser.Role switch
        {
            UserRole.AdminRaiz => true,
            UserRole.JefeArea => currentUser.AreaId == survey.AreaId,
            UserRole.Relevador => currentUser.UserId == survey.OwnerId,
            _ => false,
        };
        if (!visible)
            throw new SurveyException(SurveyErrorCode.Forbidden, "No tenés visibilidad de este relevamiento.");

        return await _db.Points.AsNoTracking()
            .Where(p => p.SurveyId == surveyId && !p.IsDeleted)
            .OrderBy(p => p.CreatedAt)
            .Select(p => new PointDto(
                p.Id,
                p.SurveyId,
                p.Latitude,
                p.Longitude,
                p.AccuracyM,
                p.Title,
                p.Description,
                p.CaptureMode,
                p.CreatedAt,
                p.UpdatedAt))
            .ToListAsync(ct);
    }
}

/// <summary>
/// Proyección plana de un Punto para listar/mostrar en mapa.
/// Coincide con el DTO espejado en el cliente móvil (<c>Sgr.Frontend.Mobile.Api.PointDto</c>).
/// </summary>
public sealed record PointDto(
    Guid Id,
    Guid SurveyId,
    decimal Latitude,
    decimal Longitude,
    decimal? AccuracyM,
    string? Title,
    string? Description,
    string CaptureMode,
    DateTime CreatedAt,
    DateTime UpdatedAt);
