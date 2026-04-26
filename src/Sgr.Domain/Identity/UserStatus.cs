namespace Sgr.Domain.Identity;

public static class UserStatus
{
    public const string PendienteAceptacion = "pendiente_aceptacion";
    public const string Activo = "activo";
    public const string Inhabilitado = "inhabilitado";
    public const string DadoDeBaja = "dado_de_baja";

    public static readonly IReadOnlySet<string> AllStatuses =
        new HashSet<string> { PendienteAceptacion, Activo, Inhabilitado, DadoDeBaja };

    public static bool IsValid(string status) => AllStatuses.Contains(status);
}
