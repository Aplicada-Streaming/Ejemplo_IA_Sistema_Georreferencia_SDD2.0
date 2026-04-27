using Sgr.Domain.Common;

namespace Sgr.Domain.Templates;

public sealed class TemplateVersion : Entity
{
    public Guid TemplateId { get; private set; }
    public int VersionNumber { get; private set; }
    public string Status { get; private set; } = default!;
    public string FieldDefinitionsJson { get; private set; } = default!;
    public string CaptureParamsJson { get; private set; } = default!;
    public DateTime? PublishedAt { get; private set; }

    private TemplateVersion() { }

    public static TemplateVersion CreateDraft(
        Guid id,
        Guid templateId,
        int versionNumber,
        string fieldDefinitionsJson,
        string captureParamsJson,
        DateTime createdAt)
    {
        if (versionNumber < 1)
            throw new ArgumentException("Version number must be >= 1.", nameof(versionNumber));
        if (string.IsNullOrWhiteSpace(fieldDefinitionsJson))
            throw new ArgumentException("Field definitions are required.", nameof(fieldDefinitionsJson));
        if (string.IsNullOrWhiteSpace(captureParamsJson))
            throw new ArgumentException("Capture params are required.", nameof(captureParamsJson));

        return new TemplateVersion
        {
            Id = id,
            TemplateId = templateId,
            VersionNumber = versionNumber,
            Status = TemplateVersionStatus.Borrador,
            FieldDefinitionsJson = fieldDefinitionsJson,
            CaptureParamsJson = captureParamsJson,
            CreatedAt = createdAt,
            PublishedAt = null,
        };
    }

    /// <summary>Publish this version. Once published, the version is immutable (RN-05).</summary>
    public void Publish(DateTime now)
    {
        if (Status == TemplateVersionStatus.Publicada)
            throw new InvalidOperationException(
                "Template version is already published. Create a new version instead.");

        Status = TemplateVersionStatus.Publicada;
        PublishedAt = now;
    }

    /// <summary>
    /// Reemplaza el JSON de definiciones de campos. Sólo permitido en estado borrador
    /// (RN-05: las versiones publicadas son inmutables).
    /// </summary>
    public void UpdateFieldDefinitions(string fieldDefinitionsJson)
    {
        if (Status != TemplateVersionStatus.Borrador)
            throw new InvalidOperationException("Sólo se pueden editar versiones en estado borrador.");
        if (string.IsNullOrWhiteSpace(fieldDefinitionsJson))
            throw new ArgumentException("Field definitions are required.", nameof(fieldDefinitionsJson));
        FieldDefinitionsJson = fieldDefinitionsJson;
    }

    /// <summary>Reemplaza el JSON de parámetros de captura. Sólo en borrador (RN-05).</summary>
    public void UpdateCaptureParams(string captureParamsJson)
    {
        if (Status != TemplateVersionStatus.Borrador)
            throw new InvalidOperationException("Sólo se pueden editar versiones en estado borrador.");
        if (string.IsNullOrWhiteSpace(captureParamsJson))
            throw new ArgumentException("Capture params are required.", nameof(captureParamsJson));
        CaptureParamsJson = captureParamsJson;
    }

    public bool IsPublished => Status == TemplateVersionStatus.Publicada;
}
