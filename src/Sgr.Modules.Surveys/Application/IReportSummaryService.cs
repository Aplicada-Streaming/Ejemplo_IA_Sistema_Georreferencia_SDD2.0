using Microsoft.EntityFrameworkCore;
using Sgr.Domain.Identity;
using Sgr.Domain.Surveys;
using Sgr.Persistence;

namespace Sgr.Modules.Surveys.Application;

public interface IReportSummaryService
{
    /// <summary>
    /// KPIs filtrados por la visibilidad del usuario actual:
    /// admin ve todo, jefe ve su área, relevador ve sus surveys.
    /// </summary>
    Task<ReportSummaryDto> GetAsync(CurrentUserContext currentUser, CancellationToken ct = default);
}

public sealed class ReportSummaryService : IReportSummaryService
{
    private readonly SgrDbContext _db;

    public ReportSummaryService(SgrDbContext db) => _db = db;

    public async Task<ReportSummaryDto> GetAsync(CurrentUserContext user, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(user);

        // Filtro de surveys según rol — mismo criterio que IListSurveysService.
        var surveysQuery = _db.Surveys.AsNoTracking().Where(s => !s.IsDeleted);
        switch (user.Role)
        {
            case UserRole.AdminRaiz: break;
            case UserRole.JefeArea when user.AreaId is not null:
                surveysQuery = surveysQuery.Where(s => s.AreaId == user.AreaId);
                break;
            case UserRole.Relevador:
                surveysQuery = surveysQuery.Where(s => s.OwnerId == user.UserId);
                break;
            default:
                return ReportSummaryDto.Empty;
        }

        var surveys = await surveysQuery
            .Select(s => new { s.Id, s.Name, s.Status, s.CreatedAt, s.UpdatedAt })
            .ToListAsync(ct);

        var visibleSurveyIds = surveys.Select(s => s.Id).ToHashSet();
        if (visibleSurveyIds.Count == 0) return ReportSummaryDto.Empty;

        var totalPoints = await _db.Points.AsNoTracking()
            .Where(p => visibleSurveyIds.Contains(p.SurveyId) && !p.IsDeleted)
            .CountAsync(ct);

        var totalPhotos = await _db.Photos.AsNoTracking()
            .Where(p => !p.IsDeleted && _db.Points.Any(pt => pt.Id == p.PointId && visibleSurveyIds.Contains(pt.SurveyId)))
            .CountAsync(ct);

        var byStatus = surveys.GroupBy(s => s.Status)
            .ToDictionary(g => g.Key, g => g.Count());

        var recent = surveys
            .OrderByDescending(s => s.UpdatedAt)
            .Take(5)
            .Select(s => new RecentSurveyDto(s.Id, s.Name, s.Status, s.UpdatedAt))
            .ToList();

        return new ReportSummaryDto(
            TotalSurveys: surveys.Count,
            OpenSurveys: byStatus.GetValueOrDefault(SurveyStatus.Abierto, 0),
            ClosedSurveys: byStatus.GetValueOrDefault(SurveyStatus.Cerrado, 0),
            TotalPoints: totalPoints,
            TotalPhotos: totalPhotos,
            Recent: recent);
    }
}

public sealed record ReportSummaryDto(
    int TotalSurveys,
    int OpenSurveys,
    int ClosedSurveys,
    int TotalPoints,
    int TotalPhotos,
    IReadOnlyList<RecentSurveyDto> Recent)
{
    public static readonly ReportSummaryDto Empty =
        new(0, 0, 0, 0, 0, Array.Empty<RecentSurveyDto>());
}

public sealed record RecentSurveyDto(
    Guid Id,
    string Name,
    string Status,
    DateTime UpdatedAt);
