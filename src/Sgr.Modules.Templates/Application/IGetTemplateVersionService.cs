using Microsoft.EntityFrameworkCore;
using Sgr.Persistence;

namespace Sgr.Modules.Templates.Application;

public interface IGetTemplateVersionService
{
    /// <summary>
    /// Devuelve el detalle de una versión: metadata + schema deserializado
    /// (campos + parámetros de captura).
    /// </summary>
    Task<TemplateVersionDetailDto> GetByIdAsync(Guid versionId, CancellationToken ct = default);

    /// <summary>
    /// Devuelve el detalle de la versión asignada a un relevamiento.
    /// Útil para el cliente móvil: con sólo el surveyId trae lo que necesita
    /// para configurar la captura.
    /// </summary>
    Task<TemplateVersionDetailDto> GetForSurveyAsync(Guid surveyId, CancellationToken ct = default);
}

public sealed class GetTemplateVersionService : IGetTemplateVersionService
{
    private readonly SgrDbContext _db;

    public GetTemplateVersionService(SgrDbContext db) => _db = db;

    public async Task<TemplateVersionDetailDto> GetByIdAsync(Guid versionId, CancellationToken ct = default)
    {
        var version = await _db.TemplateVersions.AsNoTracking()
            .FirstOrDefaultAsync(v => v.Id == versionId, ct)
            ?? throw new TemplateNotFoundException($"TemplateVersion {versionId} no existe.");

        var template = await _db.Templates.AsNoTracking()
            .FirstOrDefaultAsync(t => t.Id == version.TemplateId, ct)
            ?? throw new TemplateNotFoundException($"Template {version.TemplateId} no existe (huérfano).");

        return Build(template.Id, template.Name, version);
    }

    public async Task<TemplateVersionDetailDto> GetForSurveyAsync(Guid surveyId, CancellationToken ct = default)
    {
        var survey = await _db.Surveys.AsNoTracking()
            .Where(s => s.Id == surveyId && !s.IsDeleted)
            .Select(s => new { s.Id, s.TemplateVersionId })
            .FirstOrDefaultAsync(ct)
            ?? throw new TemplateNotFoundException($"Survey {surveyId} no existe.");

        return await GetByIdAsync(survey.TemplateVersionId, ct);
    }

    private static TemplateVersionDetailDto Build(Guid templateId, string templateName, Sgr.Domain.Templates.TemplateVersion v) =>
        new(
            VersionId: v.Id,
            TemplateId: templateId,
            TemplateName: templateName,
            VersionNumber: v.VersionNumber,
            Status: v.Status,
            PublishedAt: v.PublishedAt,
            Fields: TemplateSchemaJson.ParseFields(v.FieldDefinitionsJson),
            CaptureParams: TemplateSchemaJson.ParseCaptureParams(v.CaptureParamsJson));
}

public sealed record TemplateVersionDetailDto(
    Guid VersionId,
    Guid TemplateId,
    string TemplateName,
    int VersionNumber,
    string Status,
    DateTime? PublishedAt,
    IReadOnlyList<FieldDefinition> Fields,
    CaptureParams CaptureParams);

public sealed class TemplateNotFoundException : Exception
{
    public TemplateNotFoundException(string message) : base(message) { }
}
