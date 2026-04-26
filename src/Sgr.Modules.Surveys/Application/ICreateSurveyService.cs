using Microsoft.EntityFrameworkCore;
using Sgr.Domain.Audit;
using Sgr.Domain.Common;
using Sgr.Domain.Identity;
using Sgr.Domain.Surveys;
using Sgr.Modules.Templates.Application;
using Sgr.Persistence;

namespace Sgr.Modules.Surveys.Application;

public interface ICreateSurveyService
{
    Task<SurveyDto> CreateAsync(
        CreateSurveyRequest request,
        CurrentUserContext currentUser,
        CancellationToken ct = default);
}

public sealed class CreateSurveyService : ICreateSurveyService
{
    private readonly SgrDbContext _db;
    private readonly ITemplateVersionQuery _templateQuery;
    private readonly IDateTimeProvider _clock;

    public CreateSurveyService(
        SgrDbContext db,
        ITemplateVersionQuery templateQuery,
        IDateTimeProvider clock)
    {
        _db = db;
        _templateQuery = templateQuery;
        _clock = clock;
    }

    public async Task<SurveyDto> CreateAsync(
        CreateSurveyRequest request,
        CurrentUserContext currentUser,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(currentUser);

        if (currentUser.Role == UserRole.AdminRaiz)
            throw new SurveyException(SurveyErrorCode.Forbidden,
                "El admin raíz no crea relevamientos.");

        if (currentUser.AreaId is null)
            throw new SurveyException(SurveyErrorCode.AreaUnknown,
                "El usuario no tiene un área asignada.");

        if (request.SurveyId == Guid.Empty)
            throw new SurveyException(SurveyErrorCode.InvalidPayload,
                "El identificador del relevamiento es requerido.");

        if (string.IsNullOrWhiteSpace(request.Name))
            throw new SurveyException(SurveyErrorCode.InvalidPayload,
                "El nombre del relevamiento es requerido.");

        if (!AuditOrigin.IsValid(request.Origin))
            throw new SurveyException(SurveyErrorCode.InvalidPayload,
                $"Origen '{request.Origin}' inválido.");

        // RN-06: idempotency by GUID — repeated request with same id returns existing.
        var existing = await _db.Surveys.AsNoTracking().FirstOrDefaultAsync(s => s.Id == request.SurveyId, ct);
        if (existing is not null)
            return ToDto(existing);

        // Resolve template version (CA-2.5: no published templates → block).
        Guid templateVersionId;
        if (request.TemplateVersionId is null)
        {
            var root = await _templateQuery.FindRootPublishedAsync(ct)
                ?? throw new SurveyException(SurveyErrorCode.NoPublishedTemplateAvailable,
                    "No hay plantillas publicadas disponibles.");
            templateVersionId = root.Id;
        }
        else
        {
            var publishedExists = await _templateQuery.ExistsPublishedAsync(request.TemplateVersionId.Value, ct);
            if (!publishedExists)
                throw new SurveyException(SurveyErrorCode.TemplateVersionNotPublished,
                    "La versión de plantilla indicada no existe o no está publicada.");
            templateVersionId = request.TemplateVersionId.Value;
        }

        var now = _clock.UtcNow;
        var survey = Survey.Create(
            id: request.SurveyId,
            name: request.Name,
            description: request.Description,
            areaId: currentUser.AreaId.Value,
            ownerId: currentUser.UserId,
            templateVersionId: templateVersionId,
            tags: request.Tags,
            createdAt: now);

        // RN-10: append-only audit event for the creation.
        var auditEvent = AuditEvent.Create(
            id: Guid.NewGuid(),
            entityType: AuditEntityType.Survey,
            entityId: survey.Id,
            eventType: AuditEventType.Created,
            fieldKey: null,
            oldValueJson: null,
            newValueJson: System.Text.Json.JsonSerializer.Serialize(new
            {
                survey.Name,
                survey.Description,
                survey.AreaId,
                survey.OwnerId,
                survey.TemplateVersionId,
                survey.Status,
                survey.Tags,
            }),
            authorId: currentUser.UserId,
            origin: request.Origin,
            deviceId: request.DeviceId,
            timestampOriginal: request.TimestampOriginal == default ? now : request.TimestampOriginal,
            appliedAt: now);

        try
        {
            _db.Surveys.Add(survey);
            _db.AuditEvents.Add(auditEvent);
            await _db.SaveChangesAsync(ct);
        }
        catch (DbUpdateException)
        {
            // Concurrent insert with same GUID → reload and return existing (idempotent).
            var stored = await _db.Surveys.AsNoTracking().FirstOrDefaultAsync(s => s.Id == request.SurveyId, ct);
            if (stored is not null) return ToDto(stored);
            throw;
        }

        return ToDto(survey);
    }

    private static SurveyDto ToDto(Survey s) => new(
        s.Id, s.Name, s.Description, s.AreaId, s.OwnerId,
        s.TemplateVersionId, s.Status, s.Tags, s.CreatedAt, s.UpdatedAt, s.ClosedAt);
}
