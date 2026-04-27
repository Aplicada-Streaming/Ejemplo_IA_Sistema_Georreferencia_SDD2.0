using Sgr.Domain.Common;

namespace Sgr.Domain.MergeCandidates;

/// <summary>
/// Par de Puntos cercanos creados por colaboradores distintos en el mismo Relevamiento,
/// detectado durante el sync push (RN-09 / US-21). Estado <c>pendiente</c> hasta que un
/// admin/jefe decide vía <see cref="MergeCandidateStatus"/>.
///
/// Invariante: <see cref="PointAId"/> &lt; <see cref="PointBId"/> en orden lexicográfico.
/// Esto evita guardar duplicados (A,B) y (B,A) — el detector siempre normaliza al
/// mismo orden antes de buscar/insertar.
/// </summary>
public sealed class MergeCandidate : Entity
{
    public Guid SurveyId { get; private set; }
    public Guid PointAId { get; private set; }
    public Guid PointBId { get; private set; }
    public Guid PointACreatedBy { get; private set; }
    public Guid PointBCreatedBy { get; private set; }
    public DateTime PointACreatedAt { get; private set; }
    public DateTime PointBCreatedAt { get; private set; }

    /// <summary>Distancia geodésica en metros al momento de la detección.</summary>
    public decimal DistanceMeters { get; private set; }

    /// <summary>"pendiente" | "fusionado" | "mantenido_separado"</summary>
    public string Status { get; private set; } = default!;

    public DateTime? ResolvedAtUtc { get; private set; }
    public Guid? ResolvedBy { get; private set; }

    /// <summary>Para <c>fusionado</c>: id del Punto resultante.</summary>
    public Guid? ResultPointId { get; private set; }

    /// <summary>Para <c>fusionado</c>: "centroid" | "keep_a" | "keep_b".</summary>
    public string? ResolutionStrategy { get; private set; }

    private MergeCandidate() { }

    public static MergeCandidate Create(
        Guid id,
        Guid surveyId,
        Guid pointAId,
        Guid pointBId,
        Guid pointACreatedBy,
        Guid pointBCreatedBy,
        DateTime pointACreatedAt,
        DateTime pointBCreatedAt,
        decimal distanceMeters,
        DateTime nowUtc)
    {
        if (pointAId == pointBId)
            throw new ArgumentException("A merge candidate must reference two different points.");

        // Invariante: PointAId < PointBId. Si vienen al revés, swap antes de persistir.
        if (pointAId.CompareTo(pointBId) > 0)
        {
            (pointAId, pointBId) = (pointBId, pointAId);
            (pointACreatedBy, pointBCreatedBy) = (pointBCreatedBy, pointACreatedBy);
            (pointACreatedAt, pointBCreatedAt) = (pointBCreatedAt, pointACreatedAt);
        }

        return new MergeCandidate
        {
            Id = id,
            SurveyId = surveyId,
            PointAId = pointAId,
            PointBId = pointBId,
            PointACreatedBy = pointACreatedBy,
            PointBCreatedBy = pointBCreatedBy,
            PointACreatedAt = pointACreatedAt,
            PointBCreatedAt = pointBCreatedAt,
            DistanceMeters = distanceMeters,
            Status = MergeCandidateStatus.Pendiente,
            CreatedAt = nowUtc,
        };
    }

    public void MarkMerged(Guid resultPointId, string strategy, Guid resolvedBy, DateTime nowUtc)
    {
        if (Status != MergeCandidateStatus.Pendiente)
            throw new InvalidOperationException("Candidate already resolved.");
        if (resultPointId != PointAId && resultPointId != PointBId)
            throw new ArgumentException("ResultPointId must be one of the candidate points.");
        Status = MergeCandidateStatus.Fusionado;
        ResultPointId = resultPointId;
        ResolutionStrategy = strategy;
        ResolvedBy = resolvedBy;
        ResolvedAtUtc = nowUtc;
    }

    public void MarkKeptSeparate(Guid resolvedBy, DateTime nowUtc)
    {
        if (Status != MergeCandidateStatus.Pendiente)
            throw new InvalidOperationException("Candidate already resolved.");
        Status = MergeCandidateStatus.MantenidoSeparado;
        ResolvedBy = resolvedBy;
        ResolvedAtUtc = nowUtc;
    }
}

public static class MergeCandidateStatus
{
    public const string Pendiente = "pendiente";
    public const string Fusionado = "fusionado";
    public const string MantenidoSeparado = "mantenido_separado";
}

public static class MergeStrategies
{
    public const string Centroid = "centroid";
    public const string KeepA = "keep_a";
    public const string KeepB = "keep_b";

    public static readonly IReadOnlySet<string> All = new HashSet<string> { Centroid, KeepA, KeepB };
    public static bool IsValid(string s) => All.Contains(s);
}
