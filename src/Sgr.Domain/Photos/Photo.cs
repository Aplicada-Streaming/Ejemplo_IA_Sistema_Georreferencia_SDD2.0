using Sgr.Domain.Common;

namespace Sgr.Domain.Photos;

/// <summary>
/// Foto asociada a un Punto. La binaria vive en el adaptador de storage referenciado por
/// (<see cref="AdapterName"/>, <see cref="AdapterRef"/>). RN-12: una foto creada con un adapter
/// concreto sigue leyéndose desde ese adapter aunque la configuración del sistema cambie.
/// </summary>
public sealed class Photo : Entity
{
    /// <summary>
    /// Punto al que está asociada la foto. Es <c>null</c> cuando la foto vino por
    /// <c>web_manual_upload</c> sin EXIF y todavía está en la cola de pendientes
    /// de georreferenciar (US-16). Se llena al resolver via <see cref="AssignToPoint"/>.
    /// </summary>
    public Guid? PointId { get; private set; }
    public string? Comment { get; private set; }
    public string AdapterName { get; private set; } = default!;
    public string AdapterRef { get; private set; } = default!;
    public long SizeBytes { get; private set; }
    public string ContentHash { get; private set; } = default!;
    public string MetadataJson { get; private set; } = default!;
    public Guid CreatedBy { get; private set; }
    public string Origin { get; private set; } = default!;
    public Guid? SurveyId { get; private set; }
    public bool IsDeleted { get; private set; }
    public DateTime? DeletedAt { get; private set; }

    private Photo() { }

    public static Photo Create(
        Guid id,
        Guid pointId,
        string adapterName,
        string adapterRef,
        long sizeBytes,
        string contentHash,
        string metadataJson,
        Guid createdBy,
        string origin,
        DateTime createdAt,
        string? comment = null) =>
        CreateInternal(id, pointId, surveyId: null, adapterName, adapterRef, sizeBytes,
            contentHash, metadataJson, createdBy, origin, createdAt, comment);

    /// <summary>
    /// Crea una Foto pendiente de georreferenciar (US-15 / US-16). PointId queda <c>null</c>
    /// y se requiere <paramref name="surveyId"/> para que la cola pueda filtrar por survey.
    /// </summary>
    public static Photo CreatePendingGeo(
        Guid id,
        Guid surveyId,
        string adapterName,
        string adapterRef,
        long sizeBytes,
        string contentHash,
        string metadataJson,
        Guid createdBy,
        string origin,
        DateTime createdAt,
        string? comment = null)
    {
        if (surveyId == Guid.Empty) throw new ArgumentException("SurveyId required for pending photo.", nameof(surveyId));
        return CreateInternal(id, pointId: null, surveyId, adapterName, adapterRef, sizeBytes,
            contentHash, metadataJson, createdBy, origin, createdAt, comment);
    }

    private static Photo CreateInternal(
        Guid id,
        Guid? pointId,
        Guid? surveyId,
        string adapterName,
        string adapterRef,
        long sizeBytes,
        string contentHash,
        string metadataJson,
        Guid createdBy,
        string origin,
        DateTime createdAt,
        string? comment)
    {
        if (id == Guid.Empty) throw new ArgumentException("Photo id required.", nameof(id));
        if (pointId is null && surveyId is null)
            throw new ArgumentException("Either PointId or SurveyId is required.", nameof(pointId));
        if (pointId == Guid.Empty) throw new ArgumentException("PointId can't be empty.", nameof(pointId));
        if (createdBy == Guid.Empty) throw new ArgumentException("CreatedBy required.", nameof(createdBy));
        if (string.IsNullOrWhiteSpace(adapterName)) throw new ArgumentException("Adapter name required.", nameof(adapterName));
        if (string.IsNullOrWhiteSpace(adapterRef)) throw new ArgumentException("Adapter ref required.", nameof(adapterRef));
        if (sizeBytes <= 0) throw new ArgumentException("SizeBytes must be > 0.", nameof(sizeBytes));
        if (string.IsNullOrWhiteSpace(contentHash)) throw new ArgumentException("ContentHash required.", nameof(contentHash));
        if (!StorageAdapterNames.IsValid(adapterName))
            throw new ArgumentException($"Invalid adapter name '{adapterName}'.", nameof(adapterName));
        if (!Audit.AuditOrigin.IsValid(origin))
            throw new ArgumentException($"Invalid origin '{origin}'.", nameof(origin));

        return new Photo
        {
            Id = id,
            PointId = pointId,
            SurveyId = surveyId,
            Comment = comment,
            AdapterName = adapterName,
            AdapterRef = adapterRef,
            SizeBytes = sizeBytes,
            ContentHash = contentHash,
            MetadataJson = metadataJson,
            CreatedBy = createdBy,
            Origin = origin,
            CreatedAt = createdAt,
            IsDeleted = false,
            DeletedAt = null,
        };
    }

    /// <summary>True si la foto está en la cola de pendientes de georreferenciar (US-16).</summary>
    public bool IsPendingGeo => PointId is null;

    /// <summary>
    /// Asocia la foto a un punto (resolución de US-16). El SurveyId del placeholder se mantiene
    /// para auditoría pero PointId pasa a ser la fuente de verdad.
    /// </summary>
    public void AssignToPoint(Guid pointId)
    {
        if (pointId == Guid.Empty) throw new ArgumentException("PointId required.", nameof(pointId));
        if (PointId is not null)
            throw new InvalidOperationException("Photo is already assigned to a point.");
        PointId = pointId;
    }

    public void UpdateComment(string? comment) => Comment = comment;

    public void SoftDelete(DateTime when)
    {
        IsDeleted = true;
        DeletedAt = when;
    }
}

public static class StorageAdapterNames
{
    public const string Local = "local";
    public const string S3 = "s3";
    public const string Ftp = "ftp";
    public const string Sftp = "sftp";

    public static readonly IReadOnlySet<string> All = new HashSet<string> { Local, S3, Ftp, Sftp };
    public static bool IsValid(string s) => All.Contains(s);
}
