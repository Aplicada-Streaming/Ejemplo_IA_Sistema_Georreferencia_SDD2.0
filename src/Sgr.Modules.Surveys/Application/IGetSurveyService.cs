using Microsoft.EntityFrameworkCore;
using Sgr.Domain.Identity;
using Sgr.Persistence;

namespace Sgr.Modules.Surveys.Application;

public interface IGetSurveyService
{
    /// <summary>
    /// Devuelve el detalle de un relevamiento. Aplica el mismo filtro de visibilidad
    /// que <see cref="IListSurveysService"/>: admin todo, jefe sólo si es de su área,
    /// relevador sólo si es el dueño.
    /// </summary>
    Task<SurveyDto> GetByIdAsync(Guid surveyId, CurrentUserContext currentUser, CancellationToken ct = default);
}

public sealed class GetSurveyService : IGetSurveyService
{
    private readonly SgrDbContext _db;

    public GetSurveyService(SgrDbContext db) => _db = db;

    public async Task<SurveyDto> GetByIdAsync(
        Guid surveyId,
        CurrentUserContext currentUser,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(currentUser);

        var s = await _db.Surveys.AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == surveyId && !x.IsDeleted, ct);
        if (s is null)
            throw new SurveyException(SurveyErrorCode.NotFound, $"Survey {surveyId} no existe.");

        var visible = currentUser.Role switch
        {
            UserRole.AdminRaiz => true,
            UserRole.JefeArea => currentUser.AreaId == s.AreaId,
            UserRole.Relevador => currentUser.UserId == s.OwnerId,
            _ => false,
        };
        if (!visible)
            throw new SurveyException(SurveyErrorCode.Forbidden, "No tenés visibilidad de este relevamiento.");

        return new SurveyDto(
            s.Id, s.Name, s.Description, s.AreaId, s.OwnerId,
            s.TemplateVersionId, s.Status, s.Tags, s.CreatedAt, s.UpdatedAt, s.ClosedAt);
    }
}
