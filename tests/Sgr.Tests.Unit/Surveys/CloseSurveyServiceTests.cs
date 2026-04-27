using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Sgr.Domain.Audit;
using Sgr.Domain.Identity;
using Sgr.Domain.Surveys;
using Sgr.Modules.Surveys.Application;
using Sgr.Tests.Unit.Common;

namespace Sgr.Tests.Unit.Surveys;

public class CloseSurveyServiceTests
{
    private readonly DateTime _now = new(2026, 4, 27, 12, 0, 0, DateTimeKind.Utc);

    private (CloseSurveyService svc, Persistence.SgrDbContext db, Survey survey, Guid areaId)
        SetupOpenSurvey()
    {
        var db = TestDb.CreateContext();
        var areaId = Guid.NewGuid();
        var ownerId = Guid.NewGuid();
        var survey = Survey.Create(Guid.NewGuid(), "Test", null, areaId, ownerId, Guid.NewGuid(), null, _now);
        db.Surveys.Add(survey);
        db.SaveChanges();
        var svc = new CloseSurveyService(db, new FakeDateTimeProvider(_now.AddHours(1)));
        return (svc, db, survey, areaId);
    }

    [Fact]
    public async Task CloseAsync_admin_can_close_any_survey()
    {
        var (svc, db, survey, _) = SetupOpenSurvey();
        var admin = new CurrentUserContext(Guid.NewGuid(), UserRole.AdminRaiz, null);

        var result = await svc.CloseAsync(survey.Id, admin);

        result.Status.Should().Be(SurveyStatus.Cerrado);
        result.ClosedAt.Should().NotBeNull();
        var stored = await db.Surveys.FirstAsync(s => s.Id == survey.Id);
        stored.Status.Should().Be(SurveyStatus.Cerrado);
    }

    [Fact]
    public async Task CloseAsync_jefe_can_close_own_area_survey()
    {
        var (svc, _, survey, areaId) = SetupOpenSurvey();
        var jefe = new CurrentUserContext(Guid.NewGuid(), UserRole.JefeArea, areaId);

        var result = await svc.CloseAsync(survey.Id, jefe);

        result.Status.Should().Be(SurveyStatus.Cerrado);
    }

    [Fact]
    public async Task CloseAsync_jefe_cannot_close_other_areas_survey()
    {
        var (svc, _, survey, _) = SetupOpenSurvey();
        var jefe = new CurrentUserContext(Guid.NewGuid(), UserRole.JefeArea, Guid.NewGuid());

        var act = () => svc.CloseAsync(survey.Id, jefe);

        var ex = await act.Should().ThrowAsync<SurveyException>();
        ex.Which.Code.Should().Be(SurveyErrorCode.Forbidden);
    }

    [Fact]
    public async Task CloseAsync_relevador_cannot_close_even_if_owner()
    {
        // Relevador es dueño del survey pero no puede cerrar (RN-09: el cierre lo decide el jefe).
        var (svc, db, _, _) = SetupOpenSurvey();
        var relevadorId = Guid.NewGuid();
        var ownerSurvey = Survey.Create(Guid.NewGuid(), "Owned", null, Guid.NewGuid(),
            relevadorId, Guid.NewGuid(), null, _now);
        db.Surveys.Add(ownerSurvey);
        await db.SaveChangesAsync();

        var rel = new CurrentUserContext(relevadorId, UserRole.Relevador, null);

        var act = () => svc.CloseAsync(ownerSurvey.Id, rel);

        var ex = await act.Should().ThrowAsync<SurveyException>();
        ex.Which.Code.Should().Be(SurveyErrorCode.Forbidden);
    }

    [Fact]
    public async Task CloseAsync_throws_NotFound_when_survey_missing()
    {
        var (svc, _, _, _) = SetupOpenSurvey();
        var admin = new CurrentUserContext(Guid.NewGuid(), UserRole.AdminRaiz, null);

        var act = () => svc.CloseAsync(Guid.NewGuid(), admin);

        var ex = await act.Should().ThrowAsync<SurveyException>();
        ex.Which.Code.Should().Be(SurveyErrorCode.NotFound);
    }

    [Fact]
    public async Task CloseAsync_emits_audit_event_for_status_change()
    {
        var (svc, db, survey, _) = SetupOpenSurvey();
        var admin = new CurrentUserContext(Guid.NewGuid(), UserRole.AdminRaiz, null);

        await svc.CloseAsync(survey.Id, admin);

        var audits = await db.AuditEvents
            .Where(a => a.EntityType == AuditEntityType.Survey && a.EntityId == survey.Id)
            .ToListAsync();
        audits.Should().HaveCount(1);
        audits[0].EventType.Should().Be(AuditEventType.FieldUpdated);
        audits[0].FieldKey.Should().Be("status");
        audits[0].AuthorId.Should().Be(admin.UserId);
    }

    [Fact]
    public async Task CloseAsync_idempotent_does_not_emit_second_audit()
    {
        var (svc, db, survey, _) = SetupOpenSurvey();
        var admin = new CurrentUserContext(Guid.NewGuid(), UserRole.AdminRaiz, null);

        await svc.CloseAsync(survey.Id, admin);
        await svc.CloseAsync(survey.Id, admin);   // segundo close — no-op

        var audits = await db.AuditEvents
            .Where(a => a.EntityType == AuditEntityType.Survey && a.EntityId == survey.Id)
            .CountAsync();
        audits.Should().Be(1);
    }
}
