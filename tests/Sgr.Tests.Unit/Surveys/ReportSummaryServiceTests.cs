using FluentAssertions;
using Sgr.Domain.Audit;
using Sgr.Domain.Identity;
using Sgr.Domain.Photos;
using Sgr.Domain.Points;
using Sgr.Domain.Surveys;
using Sgr.Modules.Surveys.Application;
using Sgr.Tests.Unit.Common;

namespace Sgr.Tests.Unit.Surveys;

public class ReportSummaryServiceTests
{
    private readonly DateTime _now = new(2026, 4, 27, 12, 0, 0, DateTimeKind.Utc);

    /// <summary>
    /// Setup: 2 areas, 2 jefes, 2 relevadores, varios surveys con puntos+fotos
    /// para verificar que el filtro por rol funciona como se espera.
    /// </summary>
    private (Persistence.SgrDbContext db, Guid areaA, Guid areaB,
             Guid relA, Guid relB, Guid surveyA1, Guid surveyA2, Guid surveyB1)
        Setup()
    {
        var db = TestDb.CreateContext();
        var areaA = Guid.NewGuid();
        var areaB = Guid.NewGuid();
        var relA = Guid.NewGuid();
        var relB = Guid.NewGuid();
        var template = Guid.NewGuid();

        var sA1 = Survey.Create(Guid.NewGuid(), "A1", null, areaA, relA, template, null, _now);
        var sA2 = Survey.Create(Guid.NewGuid(), "A2", null, areaA, relA, template, null, _now.AddMinutes(1));
        sA2.Close(_now.AddHours(1));
        var sB1 = Survey.Create(Guid.NewGuid(), "B1", null, areaB, relB, template, null, _now.AddMinutes(2));
        db.Surveys.AddRange(sA1, sA2, sB1);

        // 2 puntos en A1, 1 en A2, 3 en B1
        Point P(Survey s, decimal lat) => Point.Create(Guid.NewGuid(), s.Id, lat, -60.5m, 50m,
            relA, AuditOrigin.MobileCapture, CaptureModes.Detenido, null, _now);
        var pA1a = P(sA1, -31.7m); var pA1b = P(sA1, -31.71m);
        var pA2a = P(sA2, -31.72m);
        var pB1a = P(sB1, -31.73m); var pB1b = P(sB1, -31.74m); var pB1c = P(sB1, -31.75m);
        db.Points.AddRange(pA1a, pA1b, pA2a, pB1a, pB1b, pB1c);

        // 1 foto en pA1a, 2 en pB1a → 3 fotos total
        Photo Ph(Guid pointId) => Photo.Create(Guid.NewGuid(), pointId, "local",
            $"surveys/{pointId}/x.jpg", 1024, "deadbeef", "{}", relA, AuditOrigin.MobileCapture, _now);
        db.Photos.AddRange(Ph(pA1a.Id), Ph(pB1a.Id), Ph(pB1a.Id));

        db.SaveChanges();
        return (db, areaA, areaB, relA, relB, sA1.Id, sA2.Id, sB1.Id);
    }

    [Fact]
    public async Task Admin_sees_everything()
    {
        var (db, _, _, _, _, _, _, _) = Setup();
        var svc = new ReportSummaryService(db);
        var admin = new CurrentUserContext(Guid.NewGuid(), UserRole.AdminRaiz, null);

        var s = await svc.GetAsync(admin);

        s.TotalSurveys.Should().Be(3);
        s.OpenSurveys.Should().Be(2);
        s.ClosedSurveys.Should().Be(1);
        s.TotalPoints.Should().Be(6);
        s.TotalPhotos.Should().Be(3);
        s.Recent.Should().HaveCountLessOrEqualTo(5);
    }

    [Fact]
    public async Task Jefe_sees_only_own_area()
    {
        var (db, areaA, _, _, _, _, _, _) = Setup();
        var svc = new ReportSummaryService(db);
        var jefeA = new CurrentUserContext(Guid.NewGuid(), UserRole.JefeArea, areaA);

        var s = await svc.GetAsync(jefeA);

        s.TotalSurveys.Should().Be(2);     // A1 + A2 (cerrado)
        s.OpenSurveys.Should().Be(1);
        s.ClosedSurveys.Should().Be(1);
        s.TotalPoints.Should().Be(3);      // 2 + 1
        s.TotalPhotos.Should().Be(1);
    }

    [Fact]
    public async Task Relevador_sees_only_own_surveys()
    {
        var (db, _, _, relA, _, _, _, _) = Setup();
        var svc = new ReportSummaryService(db);
        var relA_ctx = new CurrentUserContext(relA, UserRole.Relevador, null);

        var s = await svc.GetAsync(relA_ctx);

        s.TotalSurveys.Should().Be(2);     // owner = relA → A1, A2
        s.TotalPoints.Should().Be(3);
        s.TotalPhotos.Should().Be(1);
    }

    [Fact]
    public async Task Jefe_without_area_returns_empty()
    {
        var (db, _, _, _, _, _, _, _) = Setup();
        var svc = new ReportSummaryService(db);
        var jefe = new CurrentUserContext(Guid.NewGuid(), UserRole.JefeArea, null);

        var s = await svc.GetAsync(jefe);

        s.TotalSurveys.Should().Be(0);
    }

    [Fact]
    public async Task Recent_returns_at_most_5_ordered_by_updatedAt_desc()
    {
        var (db, _, _, _, _, _, _, _) = Setup();
        var svc = new ReportSummaryService(db);
        var admin = new CurrentUserContext(Guid.NewGuid(), UserRole.AdminRaiz, null);

        var s = await svc.GetAsync(admin);

        s.Recent.Should().NotBeEmpty();
        for (int i = 1; i < s.Recent.Count; i++)
            s.Recent[i - 1].UpdatedAt.Should().BeOnOrAfter(s.Recent[i].UpdatedAt);
    }

    [Fact]
    public async Task Empty_db_returns_empty_summary()
    {
        var db = TestDb.CreateContext();
        var svc = new ReportSummaryService(db);
        var admin = new CurrentUserContext(Guid.NewGuid(), UserRole.AdminRaiz, null);

        var s = await svc.GetAsync(admin);

        s.Should().Be(ReportSummaryDto.Empty);
    }
}
