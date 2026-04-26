using FluentAssertions;
using Sgr.Domain.Templates;

namespace Sgr.Tests.Unit.Templates;

public class TemplateVersionTests
{
    private readonly DateTime _now = new(2026, 4, 26, 10, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void CreateDraft_starts_with_status_borrador()
    {
        var v = TemplateVersion.CreateDraft(
            Guid.NewGuid(), Guid.NewGuid(), 1,
            "[]", "{}", _now);

        v.Status.Should().Be(TemplateVersionStatus.Borrador);
        v.PublishedAt.Should().BeNull();
        v.IsPublished.Should().BeFalse();
    }

    [Fact]
    public void Publish_sets_status_publicada_and_published_at()
    {
        var v = TemplateVersion.CreateDraft(
            Guid.NewGuid(), Guid.NewGuid(), 1, "[]", "{}", _now);

        v.Publish(_now.AddMinutes(5));

        v.Status.Should().Be(TemplateVersionStatus.Publicada);
        v.PublishedAt.Should().Be(_now.AddMinutes(5));
        v.IsPublished.Should().BeTrue();
    }

    [Fact]
    public void Publish_twice_throws_RN_05()
    {
        var v = TemplateVersion.CreateDraft(Guid.NewGuid(), Guid.NewGuid(), 1, "[]", "{}", _now);
        v.Publish(_now);
        var act = () => v.Publish(_now);
        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void Template_root_is_not_deletable()
    {
        var t = Template.CreateRoot(Guid.NewGuid(), "Raíz", _now);
        t.IsRoot.Should().BeTrue();
        t.IsDeletable.Should().BeFalse();
        t.ParentTemplateId.Should().BeNull();
    }

    [Fact]
    public void Template_child_requires_parent_id()
    {
        var parent = Guid.NewGuid();
        var t = Template.CreateChild(Guid.NewGuid(), "Inspección de puente", parent, _now);
        t.IsRoot.Should().BeFalse();
        t.IsDeletable.Should().BeTrue();
        t.ParentTemplateId.Should().Be(parent);
    }
}
