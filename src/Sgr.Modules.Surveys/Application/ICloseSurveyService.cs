using Microsoft.EntityFrameworkCore;
using Sgr.Domain.Audit;
using Sgr.Domain.Common;
using Sgr.Domain.Identity;
using Sgr.Domain.Surveys;
using Sgr.Persistence;

namespace Sgr.Modules.Surveys.Application;

public interface ICloseSurveyService
{
    /// <summary>
    /// Cierra un relevamiento. Sólo el dueño jefe del área (o admin) puede ejecutar
    /// esta acción. Idempotente: si ya está cerrado, devuelve el DTO actual sin tocar BD.
    /// Emite AuditEvent append-only.
    /// </summary>
    Task<SurveyDto> CloseAsync(Guid surveyId, CurrentUserContext currentUser, CancellationToken ct = default);
}

public sealed class CloseSurveyService : ICloseSurveyService
{
    private readonly SgrDbContext _db;
    private readonly IDateTimeProvider _clock;

    public CloseSurveyService(SgrDbContext db, IDateTimeProvider clock)
    {
        _db = db;
        _clock = clock;
    }

    public async Task<SurveyDto> CloseAsync(
        Guid surveyId,
        CurrentUserContext currentUser,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(currentUser);

        var survey = await _db.Surveys
            .FirstOrDefaultAsync(s => s.Id == surveyId && !s.IsDeleted, ct);
        if (survey is null)
            throw new SurveyException(SurveyErrorCode.NotFound, $"Survey {surveyId} no existe.");

        // Permiso: admin todo, jefe del área, NO relevadores (incluso si son dueños).
        // El relevador puede capturar pero no decide cuándo cerrar.
        var allowed = currentUser.Role switch
        {
            UserRole.AdminRaiz => true,
            UserRole.JefeArea => currentUser.AreaId == survey.AreaId,
            _ => false,
        };
        if (!allowed)
            throw new SurveyException(SurveyErrorCode.Forbidden,
                "Sólo un jefe del área (o admin) puede cerrar un relevamiento.");

        var now = _clock.UtcNow;
        var wasOpen = survey.Status == SurveyStatus.Abierto;
        survey.Close(now);

        if (wasOpen)
        {
            // Audit append-only sólo si efectivamente cambió de estado.
            _db.AuditEvents.Add(AuditEvent.Create(
                id: Guid.NewGuid(),
                entityType: AuditEntityType.Survey,
                entityId: survey.Id,
                eventType: AuditEventType.FieldUpdated,
                fieldKey: "status",
                oldValueJson: System.Text.Json.JsonSerializer.Serialize(SurveyStatus.Abierto),
                newValueJson: System.Text.Json.JsonSerializer.Serialize(SurveyStatus.Cerrado),
                authorId: currentUser.UserId,
                origin: AuditOrigin.WebEdit,
                deviceId: null,
                timestampOriginal: now,
                appliedAt: now));

            await _db.SaveChangesAsync(ct);
        }

        return new SurveyDto(
            survey.Id, survey.Name, survey.Description, survey.AreaId, survey.OwnerId,
            survey.TemplateVersionId, survey.Status, survey.Tags,
            survey.CreatedAt, survey.UpdatedAt, survey.ClosedAt);
    }
}
