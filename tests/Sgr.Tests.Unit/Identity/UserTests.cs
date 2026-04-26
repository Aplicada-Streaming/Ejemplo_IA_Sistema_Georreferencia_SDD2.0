using FluentAssertions;
using Sgr.Domain.Identity;

namespace Sgr.Tests.Unit.Identity;

public class UserTests
{
    private readonly DateTime _now = new(2026, 4, 26, 10, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void RegisterPending_creates_user_in_pendiente_aceptacion()
    {
        var user = User.RegisterPending(
            Guid.NewGuid(), "x@y", "hash", "X Y",
            UserRole.Relevador, Guid.NewGuid(), _now);

        user.Status.Should().Be(UserStatus.PendienteAceptacion);
        user.Email.Should().Be("x@y");
        user.AcceptedAt.Should().BeNull();
    }

    [Fact]
    public void RegisterPending_throws_when_role_is_admin_raiz()
    {
        var act = () => User.RegisterPending(
            Guid.NewGuid(), "x@y", "hash", "X Y",
            UserRole.AdminRaiz, null, _now);
        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void RegisterPending_throws_when_jefe_or_relevador_without_area()
    {
        var act = () => User.RegisterPending(
            Guid.NewGuid(), "x@y", "hash", "X Y",
            UserRole.JefeArea, null, _now);
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Accept_promotes_pending_user_to_activo()
    {
        var u = User.RegisterPending(
            Guid.NewGuid(), "x@y", "hash", "X Y",
            UserRole.Relevador, Guid.NewGuid(), _now);

        u.Accept(_now.AddMinutes(5));
        u.Status.Should().Be(UserStatus.Activo);
        u.AcceptedAt.Should().Be(_now.AddMinutes(5));
    }

    [Fact]
    public void Accept_throws_when_user_already_active()
    {
        var u = User.CreateAdminRoot(Guid.NewGuid(), "a@y", "h", "A", _now);
        var act = () => u.Accept(_now);
        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void Disable_only_works_on_jefe_area()
    {
        var jefe = User.RegisterPending(Guid.NewGuid(), "j@y", "h", "J",
            UserRole.JefeArea, Guid.NewGuid(), _now);
        jefe.Accept(_now);
        jefe.Disable();
        jefe.Status.Should().Be(UserStatus.Inhabilitado);

        var relev = User.RegisterPending(Guid.NewGuid(), "r@y", "h", "R",
            UserRole.Relevador, Guid.NewGuid(), _now);
        relev.Accept(_now);
        var act = () => relev.Disable();
        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void Enable_reverts_inhabilitado_to_activo()
    {
        var jefe = User.RegisterPending(Guid.NewGuid(), "j@y", "h", "J",
            UserRole.JefeArea, Guid.NewGuid(), _now);
        jefe.Accept(_now);
        jefe.Disable();
        jefe.Enable();
        jefe.Status.Should().Be(UserStatus.Activo);
    }

    [Fact]
    public void Enable_throws_when_not_inhabilitado()
    {
        var jefe = User.RegisterPending(Guid.NewGuid(), "j@y", "h", "J",
            UserRole.JefeArea, Guid.NewGuid(), _now);
        jefe.Accept(_now);
        var act = () => jefe.Enable();
        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void CanLoginFromMobile_only_active_relevador()
    {
        var areaId = Guid.NewGuid();
        var relev = User.RegisterPending(Guid.NewGuid(), "r@y", "h", "R",
            UserRole.Relevador, areaId, _now);
        relev.CanLoginFromMobile().Should().BeFalse(); // pending
        relev.Accept(_now);
        relev.CanLoginFromMobile().Should().BeTrue();

        var jefe = User.RegisterPending(Guid.NewGuid(), "j@y", "h", "J",
            UserRole.JefeArea, areaId, _now);
        jefe.Accept(_now);
        jefe.CanLoginFromMobile().Should().BeFalse();

        var admin = User.CreateAdminRoot(Guid.NewGuid(), "a@y", "h", "A", _now);
        admin.CanLoginFromMobile().Should().BeFalse();
    }

    [Fact]
    public void CreateAdminRoot_starts_active()
    {
        var admin = User.CreateAdminRoot(Guid.NewGuid(), "a@y", "h", "A", _now);
        admin.Role.Should().Be(UserRole.AdminRaiz);
        admin.Status.Should().Be(UserStatus.Activo);
        admin.AreaId.Should().BeNull();
        admin.AcceptedAt.Should().Be(_now);
    }

    [Fact]
    public void Email_is_normalized_to_lower_invariant()
    {
        var u = User.RegisterPending(Guid.NewGuid(), " A@B.com ", "h", "X",
            UserRole.Relevador, Guid.NewGuid(), _now);
        u.Email.Should().Be("a@b.com");
    }
}
