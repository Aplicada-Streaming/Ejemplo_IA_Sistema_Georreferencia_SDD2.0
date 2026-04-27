using FluentAssertions;
using Sgr.Domain.Points;

namespace Sgr.Tests.Unit.Points;

public class PointTests
{
    private readonly DateTime _now = new(2026, 4, 26, 10, 0, 0, DateTimeKind.Utc);
    private readonly Guid _surveyId = Guid.NewGuid();
    private readonly Guid _userId = Guid.NewGuid();

    [Fact]
    public void Create_uses_provided_guid_as_id()
    {
        var id = Guid.NewGuid();
        var p = Point.Create(id, _surveyId, -34.6037m, -58.3816m, 12.5m,
            _userId, "mobile_capture", "detenido", "device-1", _now);

        p.Id.Should().Be(id);
        p.SurveyId.Should().Be(_surveyId);
        p.Latitude.Should().Be(-34.6037m);
        p.Longitude.Should().Be(-58.3816m);
        p.AccuracyM.Should().Be(12.5m);
        p.CreatedBy.Should().Be(_userId);
        p.Origin.Should().Be("mobile_capture");
        p.CaptureMode.Should().Be("detenido");
        p.DeviceId.Should().Be("device-1");
        p.IsDeleted.Should().BeFalse();
        p.CreatedAt.Should().Be(_now);
        p.UpdatedAt.Should().Be(_now);
    }

    [Theory]
    [InlineData(91.0, -58.3816)]   // lat too high
    [InlineData(-91.0, -58.3816)]  // lat too low
    [InlineData(-34.6037, 181.0)]  // lng too high
    [InlineData(-34.6037, -181.0)] // lng too low
    public void Create_rejects_invalid_coords(double lat, double lng)
    {
        var act = () => Point.Create(Guid.NewGuid(), _surveyId, (decimal)lat, (decimal)lng, null,
            _userId, "web_edit", "web", null, _now);
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Create_rejects_invalid_origin()
    {
        var act = () => Point.Create(Guid.NewGuid(), _surveyId, 0m, 0m, null,
            _userId, "bogus_origin", "detenido", null, _now);
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Create_rejects_invalid_capture_mode()
    {
        var act = () => Point.Create(Guid.NewGuid(), _surveyId, 0m, 0m, null,
            _userId, "web_edit", "bogus", null, _now);
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void UpdateField_title_updates_and_bumps_updated_at()
    {
        var p = NewPoint();
        p.UpdateField("title", "Bache lado izquierdo", _now.AddMinutes(5));
        p.Title.Should().Be("Bache lado izquierdo");
        p.UpdatedAt.Should().Be(_now.AddMinutes(5));
    }

    [Fact]
    public void UpdateField_description_works()
    {
        var p = NewPoint();
        p.UpdateField("description", "Profundidad ~5cm", _now.AddMinutes(5));
        p.Description.Should().Be("Profundidad ~5cm");
    }

    [Fact]
    public void UpdateField_unknown_throws()
    {
        var p = NewPoint();
        var act = () => p.UpdateField("unknown", "x", _now);
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void UpdateCoords_sets_new_values_and_bumps_updated_at()
    {
        var p = NewPoint();
        p.UpdateCoords(-34.62m, -58.40m, 8.0m, _now.AddMinutes(10));
        p.Latitude.Should().Be(-34.62m);
        p.Longitude.Should().Be(-58.40m);
        p.AccuracyM.Should().Be(8.0m);
        p.UpdatedAt.Should().Be(_now.AddMinutes(10));
    }

    [Fact]
    public void SoftDelete_sets_flags_and_DeletedAt()
    {
        var p = NewPoint();
        p.SoftDelete(_now.AddMinutes(15));
        p.IsDeleted.Should().BeTrue();
        p.DeletedAt.Should().Be(_now.AddMinutes(15));
        p.UpdatedAt.Should().Be(_now.AddMinutes(15));
    }

    [Fact]
    public void Restore_undeletes_and_clears_DeletedAt()
    {
        var p = NewPoint();
        p.SoftDelete(_now);
        p.Restore(_now.AddMinutes(20));
        p.IsDeleted.Should().BeFalse();
        p.DeletedAt.Should().BeNull();
        p.UpdatedAt.Should().Be(_now.AddMinutes(20));
    }

    private Point NewPoint() =>
        Point.Create(Guid.NewGuid(), _surveyId, -34.6m, -58.4m, 10m,
            _userId, "mobile_capture", "detenido", "dev-1", _now);
}
