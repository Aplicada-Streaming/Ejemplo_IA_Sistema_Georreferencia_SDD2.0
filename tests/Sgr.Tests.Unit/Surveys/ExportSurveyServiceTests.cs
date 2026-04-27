using System.Text;
using System.Text.Json;
using FluentAssertions;
using Sgr.Domain.Audit;
using Sgr.Domain.Identity;
using Sgr.Domain.Photos;
using Sgr.Domain.Points;
using Sgr.Domain.Surveys;
using Sgr.Domain.Templates;
using Sgr.Modules.Surveys.Application;
using Sgr.Modules.Templates.Application;
using Sgr.Tests.Unit.Common;

namespace Sgr.Tests.Unit.Surveys;

public class ExportSurveyServiceTests
{
    private readonly DateTime _now = new(2026, 4, 27, 12, 0, 0, DateTimeKind.Utc);

    private const string TemplateFieldsJson = """
        [
          { "key": "fecha_inspeccion", "label": "Fecha", "type": "fecha", "required": true },
          { "key": "condicion_general", "label": "Condición", "type": "selección",
            "options": ["Buena","Mala"], "required": true },
          { "key": "observaciones", "label": "Obs", "type": "texto", "required": false }
        ]
        """;
    private const string TemplateCaptureParamsJson = """
        {
          "gps_timeout_seconds": 30,
          "gps_accuracy_threshold_m": 50,
          "allow_continue_with_low_accuracy": true,
          "allow_manual_coordinates_entry_mobile": false,
          "movil_radius_m": 10,
          "photo_max_long_side_px": 2048,
          "photo_jpeg_quality": 85,
          "photo_target_format": "jpg",
          "photo_keep_original": false,
          "photo_generate_thumbnail": true,
          "photo_strip_sensitive_exif": false,
          "merge_radius_m": 10,
          "merge_time_window_hours": 24
        }
        """;

    private (ExportSurveyService svc, Persistence.SgrDbContext db, Survey survey,
             Point point1, Point point2, Guid adminId)
        Setup()
    {
        var db = TestDb.CreateContext();
        var areaId = Guid.NewGuid();
        var ownerId = Guid.NewGuid();
        var adminId = Guid.NewGuid();

        // Template
        var template = Template.CreateRoot(Guid.NewGuid(), "T", _now);
        db.Templates.Add(template);
        var version = TemplateVersion.CreateDraft(Guid.NewGuid(), template.Id, 1,
            TemplateFieldsJson, TemplateCaptureParamsJson, _now);
        version.Publish(_now);
        db.TemplateVersions.Add(version);

        // Survey + 2 puntos
        var survey = Survey.Create(Guid.NewGuid(), "Survey X", "desc",
            areaId, ownerId, version.Id, "tag", _now);
        db.Surveys.Add(survey);

        var p1 = Point.Create(Guid.NewGuid(), survey.Id, -31.7m, -60.5m, 25.5m,
            ownerId, AuditOrigin.MobileCapture, CaptureModes.Detenido, "dev1", _now);
        var p2 = Point.Create(Guid.NewGuid(), survey.Id, -31.71m, -60.51m, null,
            ownerId, AuditOrigin.MobileCapture, CaptureModes.Movil, "dev1", _now.AddMinutes(5));
        db.Points.AddRange(p1, p2);

        // Field values en p1: fecha + condicion. p2 sin valores (probaremos celdas vacías).
        db.PointFieldValues.AddRange(
            PointFieldValue.Create(Guid.NewGuid(), p1.Id, "fecha_inspeccion",
                "\"2026-04-27\"", ownerId, _now),
            PointFieldValue.Create(Guid.NewGuid(), p1.Id, "condicion_general",
                "\"Buena\"", ownerId, _now));

        // 2 fotos en p1
        db.Photos.AddRange(
            Photo.Create(Guid.NewGuid(), p1.Id, "local", $"surveys/x/p1/a.jpg",
                100, "abc", "{}", ownerId, AuditOrigin.MobileCapture, _now),
            Photo.Create(Guid.NewGuid(), p1.Id, "local", $"surveys/x/p1/b.jpg",
                200, "def", "{}", ownerId, AuditOrigin.MobileCapture, _now));

        db.SaveChanges();

        var getSurvey = new GetSurveyService(db);
        var getTemplate = new GetTemplateVersionService(db);
        var svc = new ExportSurveyService(db, getSurvey, getTemplate);

        return (svc, db, survey, p1, p2, adminId);
    }

    private static CurrentUserContext Admin(Guid userId) =>
        new(userId, UserRole.AdminRaiz, null);

    [Fact]
    public async Task GenerateCsv_uses_semicolon_separator_and_BOM()
    {
        var (svc, _, survey, _, _, adminId) = Setup();

        var bytes = await svc.GenerateCsvAsync(survey.Id, Admin(adminId));

        // BOM UTF-8
        bytes.Take(3).Should().Equal(new byte[] { 0xEF, 0xBB, 0xBF });
        var content = Encoding.UTF8.GetString(bytes, 3, bytes.Length - 3);
        content.Should().Contain("id;latitude;longitude");
        content.Should().Contain(";fecha_inspeccion;condicion_general;observaciones");
    }

    [Fact]
    public async Task GenerateCsv_includes_one_row_per_point_with_field_values()
    {
        var (svc, _, survey, p1, p2, adminId) = Setup();

        var bytes = await svc.GenerateCsvAsync(survey.Id, Admin(adminId));
        var content = Encoding.UTF8.GetString(bytes, 3, bytes.Length - 3);
        var lines = content.Split('\n', StringSplitOptions.RemoveEmptyEntries)
            .Select(l => l.TrimEnd('\r')).ToArray();

        // header + 2 data rows
        lines.Should().HaveCount(3);

        // p1 row: lat/lng/accuracy/photoCount=2 + fecha_inspeccion="2026-04-27" + condicion="Buena"
        var p1Line = lines.Skip(1).First(l => l.Contains(p1.Id.ToString()));
        p1Line.Should().Contain("-31.700000;-60.500000;25.50;detenido");
        p1Line.Should().Contain(";2;");                  // photoCount
        p1Line.Should().Contain(";2026-04-27;");
        p1Line.Should().Contain(";Buena;");

        // p2 row: sin field values, sin fotos → todos los campos vacíos al final
        var p2Line = lines.Skip(1).First(l => l.Contains(p2.Id.ToString()));
        p2Line.Should().Contain(";movil;");
        p2Line.Should().EndWith(";0;;;");                // photoCount=0 + 3 fields vacíos
    }

    [Fact]
    public async Task GenerateGeoJson_is_valid_FeatureCollection()
    {
        var (svc, _, survey, _, _, adminId) = Setup();

        var bytes = await svc.GenerateGeoJsonAsync(survey.Id, Admin(adminId));

        using var doc = JsonDocument.Parse(bytes);
        var root = doc.RootElement;

        root.GetProperty("type").GetString().Should().Be("FeatureCollection");
        root.GetProperty("metadata").GetProperty("surveyId").GetString().Should().Be(survey.Id.ToString());
        root.GetProperty("metadata").GetProperty("pointCount").GetInt32().Should().Be(2);

        var features = root.GetProperty("features").EnumerateArray().ToList();
        features.Should().HaveCount(2);

        // Cada feature tiene Point geometry con [lng, lat]
        var f1 = features[0];
        f1.GetProperty("type").GetString().Should().Be("Feature");
        f1.GetProperty("geometry").GetProperty("type").GetString().Should().Be("Point");
        var coords = f1.GetProperty("geometry").GetProperty("coordinates").EnumerateArray().ToList();
        coords.Should().HaveCount(2);
        coords[0].GetDouble().Should().BeApproximately(-60.5, 0.001);   // lng primero
        coords[1].GetDouble().Should().BeApproximately(-31.7, 0.001);   // lat después
    }

    [Fact]
    public async Task GenerateGeoJson_features_include_field_values()
    {
        var (svc, _, survey, p1, _, adminId) = Setup();

        var bytes = await svc.GenerateGeoJsonAsync(survey.Id, Admin(adminId));
        using var doc = JsonDocument.Parse(bytes);
        var f1 = doc.RootElement.GetProperty("features").EnumerateArray()
            .First(f => f.GetProperty("properties").GetProperty("id").GetString() == p1.Id.ToString());
        var props = f1.GetProperty("properties");

        props.GetProperty("fecha_inspeccion").GetString().Should().Be("2026-04-27");
        props.GetProperty("condicion_general").GetString().Should().Be("Buena");
        props.GetProperty("photoCount").GetInt32().Should().Be(2);
        props.GetProperty("captureMode").GetString().Should().Be("detenido");
    }

    [Fact]
    public async Task GenerateXlsx_returns_valid_xlsx_bytes()
    {
        var (svc, _, survey, _, _, adminId) = Setup();

        var bytes = await svc.GenerateXlsxAsync(survey.Id, Admin(adminId));

        // XLSX = ZIP container; debe empezar con la signatura "PK"
        bytes.Length.Should().BeGreaterThan(2);
        bytes[0].Should().Be(0x50);  // 'P'
        bytes[1].Should().Be(0x4B);  // 'K'
    }

    [Fact]
    public async Task Export_throws_NotFound_for_missing_survey()
    {
        var (svc, _, _, _, _, adminId) = Setup();

        var act = () => svc.GenerateCsvAsync(Guid.NewGuid(), Admin(adminId));

        await act.Should().ThrowAsync<SurveyException>();
    }

    [Fact]
    public async Task Export_respects_visibility_relevador_cannot_see_others()
    {
        var (svc, _, survey, _, _, _) = Setup();
        var otherRelevador = new CurrentUserContext(Guid.NewGuid(), UserRole.Relevador, null);

        var act = () => svc.GenerateCsvAsync(survey.Id, otherRelevador);

        var ex = await act.Should().ThrowAsync<SurveyException>();
        ex.Which.Code.Should().Be(SurveyErrorCode.Forbidden);
    }
}
