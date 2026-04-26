namespace Sgr.Domain.Identity;

public static class UserRole
{
    public const string AdminRaiz = "admin_raiz";
    public const string JefeArea = "jefe_area";
    public const string Relevador = "relevador";

    public static readonly IReadOnlySet<string> AllRoles =
        new HashSet<string> { AdminRaiz, JefeArea, Relevador };

    public static bool IsValid(string role) => AllRoles.Contains(role);
}
