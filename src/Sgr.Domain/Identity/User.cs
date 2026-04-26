using Sgr.Domain.Common;

namespace Sgr.Domain.Identity;

public sealed class User : Entity
{
    public string Email { get; private set; } = default!;
    public string PasswordHash { get; private set; } = default!;
    public string FullName { get; private set; } = default!;
    public string Role { get; private set; } = default!;
    public string Status { get; private set; } = default!;
    public Guid? AreaId { get; private set; }
    public DateTime? AcceptedAt { get; private set; }

    private User() { }

    public static User RegisterPending(
        Guid id,
        string email,
        string passwordHash,
        string fullName,
        string role,
        Guid? areaId,
        DateTime createdAt)
    {
        if (!UserRole.IsValid(role))
            throw new ArgumentException($"Invalid role '{role}'.", nameof(role));

        if (role == UserRole.AdminRaiz)
            throw new InvalidOperationException("Admin raíz cannot be registered; it is initialized at first run.");

        if (role != UserRole.AdminRaiz && areaId is null)
            throw new ArgumentException($"Role '{role}' requires an area.", nameof(areaId));

        ValidateEmail(email);

        return new User
        {
            Id = id,
            Email = email.Trim().ToLowerInvariant(),
            PasswordHash = passwordHash,
            FullName = fullName.Trim(),
            Role = role,
            Status = UserStatus.PendienteAceptacion,
            AreaId = areaId,
            CreatedAt = createdAt,
            AcceptedAt = null,
        };
    }

    public static User CreateAdminRoot(
        Guid id,
        string email,
        string passwordHash,
        string fullName,
        DateTime createdAt) =>
        new()
        {
            Id = id,
            Email = email.Trim().ToLowerInvariant(),
            PasswordHash = passwordHash,
            FullName = fullName.Trim(),
            Role = UserRole.AdminRaiz,
            Status = UserStatus.Activo,
            AreaId = null,
            CreatedAt = createdAt,
            AcceptedAt = createdAt,
        };

    public void Accept(DateTime acceptedAt)
    {
        if (Status != UserStatus.PendienteAceptacion)
            throw new InvalidOperationException(
                $"Cannot accept user in status '{Status}'.");

        Status = UserStatus.Activo;
        AcceptedAt = acceptedAt;
    }

    public void Disable()
    {
        if (Role != UserRole.JefeArea)
            throw new InvalidOperationException(
                "Only jefe_area users can be disabled.");
        if (Status == UserStatus.DadoDeBaja)
            throw new InvalidOperationException(
                "Cannot disable a dropped user.");

        Status = UserStatus.Inhabilitado;
    }

    public void Enable()
    {
        if (Status != UserStatus.Inhabilitado)
            throw new InvalidOperationException(
                "Only inhabilitado users can be re-enabled.");

        Status = UserStatus.Activo;
    }

    public void DropOff()
    {
        Status = UserStatus.DadoDeBaja;
    }

    public bool CanLoginFromMobile() =>
        Role == UserRole.Relevador && Status == UserStatus.Activo;

    public bool CanLoginFromWeb() =>
        Status == UserStatus.Activo;

    private static void ValidateEmail(string email)
    {
        if (string.IsNullOrWhiteSpace(email))
            throw new ArgumentException("Email is required.", nameof(email));
        if (!email.Contains('@'))
            throw new ArgumentException("Email is not valid.", nameof(email));
    }
}
