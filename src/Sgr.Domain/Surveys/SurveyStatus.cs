namespace Sgr.Domain.Surveys;

public static class SurveyStatus
{
    public const string Abierto = "abierto";
    public const string Cerrado = "cerrado";
    public const string EliminadoLogico = "eliminado_logico";

    public static readonly IReadOnlySet<string> All =
        new HashSet<string> { Abierto, Cerrado, EliminadoLogico };

    public static bool IsValid(string s) => All.Contains(s);
}
