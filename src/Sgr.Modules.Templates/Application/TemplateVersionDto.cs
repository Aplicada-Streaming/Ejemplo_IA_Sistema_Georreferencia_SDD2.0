namespace Sgr.Modules.Templates.Application;

public sealed record TemplateVersionDto(
    Guid Id,
    Guid TemplateId,
    string TemplateName,
    int VersionNumber,
    string Status,
    DateTime? PublishedAt);
