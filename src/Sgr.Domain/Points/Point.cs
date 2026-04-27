using Sgr.Domain.Common;

namespace Sgr.Domain.Points;

/// <summary>
/// Punto georreferenciado dentro de un Relevamiento.
/// En Slice 1 (US-03/04/05) los puntos son "vacíos": coordenadas + metadata de origen,
/// sin foto ni valores de campo de plantilla. Esos llegan en Slice 2 con la captura real.
/// El Id viene generado en cliente (RN-06) para sostener offline + idempotencia.
/// </summary>
public sealed class Point : Entity
{
    public Guid SurveyId { get; private set; }
    public decimal Latitude { get; private set; }
    public decimal Longitude { get; private set; }
    public decimal? AccuracyM { get; private set; }
    public string? Title { get; private set; }
    public string? Description { get; private set; }
    public Guid CreatedBy { get; private set; }
    public string Origin { get; private set; } = default!;
    public string CaptureMode { get; private set; } = default!;
    public string? DeviceId { get; private set; }
    public DateTime UpdatedAt { get; private set; }
    public bool IsDeleted { get; private set; }
    public DateTime? DeletedAt { get; private set; }

    private Point() { }

    public static Point Create(
        Guid id,
        Guid surveyId,
        decimal latitude,
        decimal longitude,
        decimal? accuracyM,
        Guid createdBy,
        string origin,
        string captureMode,
        string? deviceId,
        DateTime createdAt)
    {
        if (id == Guid.Empty) throw new ArgumentException("Point id is required.", nameof(id));
        if (surveyId == Guid.Empty) throw new ArgumentException("SurveyId is required.", nameof(surveyId));
        if (createdBy == Guid.Empty) throw new ArgumentException("CreatedBy is required.", nameof(createdBy));
        if (latitude is < -90 or > 90)
            throw new ArgumentException("Latitude must be in [-90, 90].", nameof(latitude));
        if (longitude is < -180 or > 180)
            throw new ArgumentException("Longitude must be in [-180, 180].", nameof(longitude));

        if (!Audit.AuditOrigin.IsValid(origin))
            throw new ArgumentException($"Invalid origin '{origin}'.", nameof(origin));
        if (!CaptureModes.IsValid(captureMode))
            throw new ArgumentException($"Invalid capture mode '{captureMode}'.", nameof(captureMode));

        return new Point
        {
            Id = id,
            SurveyId = surveyId,
            Latitude = latitude,
            Longitude = longitude,
            AccuracyM = accuracyM,
            CreatedBy = createdBy,
            Origin = origin,
            CaptureMode = captureMode,
            DeviceId = deviceId,
            Title = null,
            Description = null,
            CreatedAt = createdAt,
            UpdatedAt = createdAt,
            IsDeleted = false,
            DeletedAt = null,
        };
    }

    public void UpdateField(string fieldKey, string? newValue, DateTime updatedAt)
    {
        switch (fieldKey)
        {
            case "title":
                Title = newValue;
                break;
            case "description":
                Description = newValue;
                break;
            default:
                throw new ArgumentException($"Field '{fieldKey}' is not editable on Point.", nameof(fieldKey));
        }
        UpdatedAt = updatedAt;
    }

    public void UpdateCoords(decimal latitude, decimal longitude, decimal? accuracyM, DateTime updatedAt)
    {
        if (latitude is < -90 or > 90)
            throw new ArgumentException("Latitude must be in [-90, 90].", nameof(latitude));
        if (longitude is < -180 or > 180)
            throw new ArgumentException("Longitude must be in [-180, 180].", nameof(longitude));

        Latitude = latitude;
        Longitude = longitude;
        AccuracyM = accuracyM;
        UpdatedAt = updatedAt;
    }

    public void SoftDelete(DateTime when)
    {
        IsDeleted = true;
        DeletedAt = when;
        UpdatedAt = when;
    }

    public void Restore(DateTime when)
    {
        IsDeleted = false;
        DeletedAt = null;
        UpdatedAt = when;
    }
}

public static class CaptureModes
{
    public const string Detenido = "detenido";
    public const string Movil = "movil";
    public const string Web = "web";

    public static readonly IReadOnlySet<string> All = new HashSet<string> { Detenido, Movil, Web };
    public static bool IsValid(string s) => All.Contains(s);
}
