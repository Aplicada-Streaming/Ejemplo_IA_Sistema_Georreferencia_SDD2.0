using FluentAssertions;
using Sgr.Domain.Surveys;

namespace Sgr.Tests.Unit.Surveys;

public class SurveyTests
{
    private readonly DateTime _now = new(2026, 4, 26, 10, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void Create_starts_in_abierto_status()
    {
        var s = Survey.Create(
            Guid.NewGuid(), "Inspección Puente A",
            "Descripción", Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(),
            tags: "puente,zona-norte", _now);

        s.Status.Should().Be(SurveyStatus.Abierto);
        s.IsDeleted.Should().BeFalse();
        s.CreatedAt.Should().Be(_now);
        s.UpdatedAt.Should().Be(_now);
        s.ClosedAt.Should().BeNull();
        s.Tags.Should().Be("puente,zona-norte");
    }

    [Fact]
    public void Create_throws_when_name_is_empty()
    {
        var act = () => Survey.Create(
            Guid.NewGuid(), "  ",
            null, Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), null, _now);
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Create_throws_when_id_is_empty()
    {
        var act = () => Survey.Create(
            Guid.Empty, "X",
            null, Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), null, _now);
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Create_uses_provided_guid_as_id()
    {
        var id = Guid.NewGuid();
        var s = Survey.Create(id, "X", null, Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), null, _now);
        s.Id.Should().Be(id);
    }

    // ───── Close (E.4) ─────

    [Fact]
    public void Close_transitions_abierto_to_cerrado_and_sets_ClosedAt()
    {
        var s = Survey.Create(Guid.NewGuid(), "X", null, Guid.NewGuid(),
            Guid.NewGuid(), Guid.NewGuid(), null, _now);
        var later = _now.AddHours(2);

        s.Close(later);

        s.Status.Should().Be(SurveyStatus.Cerrado);
        s.ClosedAt.Should().Be(later);
        s.UpdatedAt.Should().Be(later);
    }

    [Fact]
    public void Close_is_idempotent_on_already_closed()
    {
        var s = Survey.Create(Guid.NewGuid(), "X", null, Guid.NewGuid(),
            Guid.NewGuid(), Guid.NewGuid(), null, _now);
        var firstClose = _now.AddHours(1);
        s.Close(firstClose);

        // Segundo close no debería tirar y no debería pisar el ClosedAt original
        // (la operación es no-op cuando el survey ya está cerrado).
        var act = () => s.Close(_now.AddHours(5));
        act.Should().NotThrow();
        s.ClosedAt.Should().Be(firstClose);
        s.Status.Should().Be(SurveyStatus.Cerrado);
    }

    [Fact]
    public void Close_throws_when_survey_is_eliminado_logico()
    {
        // Construimos un survey y forzamos status via reflexión para simular eliminado lógico
        // (no hay método público que transicione a EliminadoLogico todavía, pero la guard
        // del Close debe protegerlo igual por completitud).
        var s = Survey.Create(Guid.NewGuid(), "X", null, Guid.NewGuid(),
            Guid.NewGuid(), Guid.NewGuid(), null, _now);
        var statusProp = typeof(Survey).GetProperty("Status",
            System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public);
        statusProp!.SetValue(s, SurveyStatus.EliminadoLogico);

        var act = () => s.Close(_now.AddHours(1));
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*eliminado*");
    }
}
