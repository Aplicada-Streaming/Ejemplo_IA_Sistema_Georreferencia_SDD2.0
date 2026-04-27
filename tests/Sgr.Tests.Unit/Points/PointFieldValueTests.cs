using FluentAssertions;
using Sgr.Domain.Points;

namespace Sgr.Tests.Unit.Points;

public class PointFieldValueTests
{
    private readonly DateTime _now = new(2026, 4, 27, 12, 0, 0, DateTimeKind.Utc);
    private readonly Guid _pointId = Guid.NewGuid();
    private readonly Guid _user = Guid.NewGuid();

    [Fact]
    public void Create_persists_all_fields()
    {
        var id = Guid.NewGuid();
        var v = PointFieldValue.Create(id, _pointId, "fecha_inspeccion", "\"2026-04-27\"", _user, _now);

        v.Id.Should().Be(id);
        v.PointId.Should().Be(_pointId);
        v.FieldKey.Should().Be("fecha_inspeccion");
        v.ValueJson.Should().Be("\"2026-04-27\"");
        v.UpdatedBy.Should().Be(_user);
        v.UpdatedAt.Should().Be(_now);
        v.CreatedAt.Should().Be(_now);
    }

    [Fact]
    public void Create_allows_null_value()
    {
        var v = PointFieldValue.Create(Guid.NewGuid(), _pointId, "k", null, _user, _now);
        v.ValueJson.Should().BeNull();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_throws_when_field_key_empty(string key)
    {
        var act = () => PointFieldValue.Create(Guid.NewGuid(), _pointId, key, null, _user, _now);
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Create_throws_when_id_empty()
    {
        var act = () => PointFieldValue.Create(Guid.Empty, _pointId, "k", null, _user, _now);
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Create_throws_when_pointId_empty()
    {
        var act = () => PointFieldValue.Create(Guid.NewGuid(), Guid.Empty, "k", null, _user, _now);
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Create_throws_when_updatedBy_empty()
    {
        var act = () => PointFieldValue.Create(Guid.NewGuid(), _pointId, "k", null, Guid.Empty, _now);
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void UpdateValue_overwrites_value_and_metadata()
    {
        var v = PointFieldValue.Create(Guid.NewGuid(), _pointId, "condicion_general",
            "\"Buena\"", _user, _now);
        var newUser = Guid.NewGuid();
        var later = _now.AddHours(1);

        v.UpdateValue("\"Mala\"", newUser, later);

        v.ValueJson.Should().Be("\"Mala\"");
        v.UpdatedBy.Should().Be(newUser);
        v.UpdatedAt.Should().Be(later);
        v.CreatedAt.Should().Be(_now);     // CreatedAt no cambia en updates
        v.FieldKey.Should().Be("condicion_general");  // FieldKey no se reasigna
    }

    [Fact]
    public void UpdateValue_throws_when_updatedBy_empty()
    {
        var v = PointFieldValue.Create(Guid.NewGuid(), _pointId, "k", null, _user, _now);
        var act = () => v.UpdateValue("\"x\"", Guid.Empty, _now);
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void UpdateValue_allows_null_to_clear()
    {
        var v = PointFieldValue.Create(Guid.NewGuid(), _pointId, "obs", "\"texto\"", _user, _now);
        v.UpdateValue(null, _user, _now.AddMinutes(1));
        v.ValueJson.Should().BeNull();
    }
}
