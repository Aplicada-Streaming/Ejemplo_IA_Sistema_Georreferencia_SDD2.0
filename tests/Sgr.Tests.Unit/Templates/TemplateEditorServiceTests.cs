using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Sgr.Domain.Templates;
using Sgr.Modules.Templates.Application;
using Sgr.Tests.Unit.Common;

namespace Sgr.Tests.Unit.Templates;

public class TemplateEditorServiceTests
{
    private const string SampleFieldsJson =
        "[{\"key\":\"k1\",\"label\":\"K1\",\"type\":\"texto\",\"required\":false}]";
    private const string SampleCaptureParamsJson =
        "{\"gps_timeout_seconds\":30,\"gps_accuracy_threshold_m\":50}";

    private (TemplateEditorService svc, Persistence.SgrDbContext db, FakeDateTimeProvider clock)
        SetupWithSeededRoot()
    {
        var db = TestDb.CreateContext();
        var clock = new FakeDateTimeProvider(new DateTime(2026, 4, 27, 12, 0, 0, DateTimeKind.Utc));

        // Seed plantilla raíz publicada (precondición de CreateTemplate).
        var root = Template.CreateRoot(Guid.NewGuid(), "Raíz", clock.UtcNow);
        db.Templates.Add(root);
        var rootV1 = TemplateVersion.CreateDraft(
            Guid.NewGuid(), root.Id, 1, SampleFieldsJson, SampleCaptureParamsJson, clock.UtcNow);
        rootV1.Publish(clock.UtcNow);
        db.TemplateVersions.Add(rootV1);
        db.SaveChanges();

        var svc = new TemplateEditorService(db, clock);
        return (svc, db, clock);
    }

    [Fact]
    public async Task CreateTemplate_creates_child_with_v1_draft_cloned_from_root()
    {
        var (svc, db, _) = SetupWithSeededRoot();

        var result = await svc.CreateTemplateAsync("Plantilla puentes");

        var template = await db.Templates.FirstAsync(t => t.Id == result.TemplateId);
        template.Name.Should().Be("Plantilla puentes");
        template.IsRoot.Should().BeFalse();
        template.IsDeletable.Should().BeTrue();
        template.ParentTemplateId.Should().NotBeNull();

        var version = await db.TemplateVersions.FirstAsync(v => v.Id == result.DraftVersionId);
        version.TemplateId.Should().Be(result.TemplateId);
        version.VersionNumber.Should().Be(1);
        version.Status.Should().Be(TemplateVersionStatus.Borrador);
        // Schema clonado de la raíz
        version.FieldDefinitionsJson.Should().Be(SampleFieldsJson);
        version.CaptureParamsJson.Should().Be(SampleCaptureParamsJson);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task CreateTemplate_throws_on_empty_name(string name)
    {
        var (svc, _, _) = SetupWithSeededRoot();

        var act = () => svc.CreateTemplateAsync(name);

        await act.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task CreateTemplate_throws_when_no_root_exists()
    {
        var db = TestDb.CreateContext();   // sin seed
        var clock = new FakeDateTimeProvider(DateTime.UtcNow);
        var svc = new TemplateEditorService(db, clock);

        var act = () => svc.CreateTemplateAsync("X");

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*raíz*");
    }

    [Fact]
    public async Task UpdateFields_persists_new_json()
    {
        var (svc, db, _) = SetupWithSeededRoot();
        var created = await svc.CreateTemplateAsync("X");
        var newJson = "[{\"key\":\"new\",\"label\":\"Nuevo\",\"type\":\"texto\",\"required\":true}]";

        await svc.UpdateFieldsAsync(created.DraftVersionId, newJson);

        var v = await db.TemplateVersions.FirstAsync(x => x.Id == created.DraftVersionId);
        v.FieldDefinitionsJson.Should().Be(newJson);
    }

    [Fact]
    public async Task UpdateFields_throws_when_version_publicada_RN_05()
    {
        var (svc, _, _) = SetupWithSeededRoot();
        var created = await svc.CreateTemplateAsync("X");
        await svc.PublishAsync(created.DraftVersionId);

        var act = () => svc.UpdateFieldsAsync(created.DraftVersionId, "[]");

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*borrador*");
    }

    [Fact]
    public async Task UpdateFields_throws_when_version_not_found()
    {
        var (svc, _, _) = SetupWithSeededRoot();

        var act = () => svc.UpdateFieldsAsync(Guid.NewGuid(), "[]");

        await act.Should().ThrowAsync<TemplateNotFoundException>();
    }

    [Fact]
    public async Task UpdateCaptureParams_persists_new_json()
    {
        var (svc, db, _) = SetupWithSeededRoot();
        var created = await svc.CreateTemplateAsync("X");
        var newJson = "{\"gps_timeout_seconds\":15}";

        await svc.UpdateCaptureParamsAsync(created.DraftVersionId, newJson);

        var v = await db.TemplateVersions.FirstAsync(x => x.Id == created.DraftVersionId);
        v.CaptureParamsJson.Should().Be(newJson);
    }

    [Fact]
    public async Task PublishAsync_transitions_to_publicada()
    {
        var (svc, db, clock) = SetupWithSeededRoot();
        var created = await svc.CreateTemplateAsync("X");
        clock.Advance(TimeSpan.FromMinutes(5));

        await svc.PublishAsync(created.DraftVersionId);

        var v = await db.TemplateVersions.FirstAsync(x => x.Id == created.DraftVersionId);
        v.Status.Should().Be(TemplateVersionStatus.Publicada);
        v.PublishedAt.Should().Be(clock.UtcNow);
    }

    [Fact]
    public async Task PublishAsync_is_idempotent_on_already_published()
    {
        var (svc, _, _) = SetupWithSeededRoot();
        var created = await svc.CreateTemplateAsync("X");
        await svc.PublishAsync(created.DraftVersionId);

        var act = () => svc.PublishAsync(created.DraftVersionId);

        await act.Should().NotThrowAsync();
    }
}
