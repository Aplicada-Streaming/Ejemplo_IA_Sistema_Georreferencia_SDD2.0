using Microsoft.EntityFrameworkCore;
using Sgr.Domain.Identity;
using Sgr.Domain.Surveys;
using Sgr.Persistence;

namespace Sgr.Modules.Surveys.Application;

public interface IListSurveysService
{
    Task<IReadOnlyList<SurveyDto>> ListAsync(
        CurrentUserContext currentUser,
        string? statusFilter,
        CancellationToken ct = default);
}

public sealed class ListSurveysService : IListSurveysService
{
    private readonly SgrDbContext _db;

    public ListSurveysService(SgrDbContext db) => _db = db;

    public async Task<IReadOnlyList<SurveyDto>> ListAsync(
        CurrentUserContext currentUser,
        string? statusFilter,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(currentUser);

        var query = _db.Surveys.AsNoTracking().Where(s => !s.IsDeleted);

        // Sprint 0 walking skeleton scope: simple visibility rules.
        // The full filter logic (CA-5.4 jefe sees own area, relevador sees own/collaborator)
        // is implemented in US-05/US-09. For now:
        //   - admin_raiz: everything
        //   - jefe_area: surveys of own area
        //   - relevador: surveys where they are the owner
        switch (currentUser.Role)
        {
            case UserRole.AdminRaiz:
                break; // no extra filter
            case UserRole.JefeArea when currentUser.AreaId is not null:
                query = query.Where(s => s.AreaId == currentUser.AreaId);
                break;
            case UserRole.Relevador:
                query = query.Where(s => s.OwnerId == currentUser.UserId);
                break;
            default:
                return Array.Empty<SurveyDto>();
        }

        if (!string.IsNullOrWhiteSpace(statusFilter))
        {
            if (!SurveyStatus.IsValid(statusFilter))
                throw new SurveyException(SurveyErrorCode.InvalidPayload,
                    $"Estado '{statusFilter}' inválido.");
            query = query.Where(s => s.Status == statusFilter);
        }

        return await query
            .OrderByDescending(s => s.CreatedAt)
            .Select(s => new SurveyDto(
                s.Id, s.Name, s.Description, s.AreaId, s.OwnerId,
                s.TemplateVersionId, s.Status, s.Tags, s.CreatedAt, s.UpdatedAt, s.ClosedAt))
            .ToListAsync(ct);
    }
}
