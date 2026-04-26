namespace Sgr.Modules.Surveys.Application;

public sealed record CreateSurveyRequest(
    Guid SurveyId,
    string Name,
    string? Description,
    Guid? TemplateVersionId,
    string? Tags,
    string Origin,
    string? DeviceId,
    DateTime TimestampOriginal);
