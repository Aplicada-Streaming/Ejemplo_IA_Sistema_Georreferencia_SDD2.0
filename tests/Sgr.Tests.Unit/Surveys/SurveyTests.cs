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
}
