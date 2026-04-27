using Sgr.Domain.Common;

namespace Sgr.Domain.Photos;

/// <summary>
/// Foto asociada a un Punto. La binaria vive en el adaptador de storage referenciado por
/// (<see cref="AdapterName"/>, <see cref="AdapterRef"/>). RN-12: una foto creada con un adapter
/// concreto sigue leyéndose desde ese adapter aunque la configuración del sistema cambie.
/// </summary>
public sealed class Photo : Entity
{
    public Guid PointId { get; private set; }
    public string? Comment { get; private set; }
    public string AdapterName { get; private set; } = default!;
    public string AdapterRef { get; private set; } = default!;
    public long SizeBytes { get; private set; }
    public string ContentHash { get; private set; } = default!;
    public string MetadataJson { get; private set; } = default!;
    public Guid CreatedBy { get; private set; }
    public string Origin { get; private set; } = default!;
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
        string? comment = null)
    {
        if (id == Guid.Empty) throw new ArgumentException("Photo id required.", nameof(id));
        if (pointId == Guid.Empty) throw new ArgumentException("PointId required.", nameof(pointId));
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
