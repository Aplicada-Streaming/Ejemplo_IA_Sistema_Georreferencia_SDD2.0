namespace Sgr.Domain.Templates;

public static class TemplateVersionStatus
{
    public const string Borrador = "borrador";
    public const string Publicada = "publicada";

    public static readonly IReadOnlySet<string> All = new HashSet<string> { Borrador, Publicada };
    public static bool IsValid(string s) => All.Contains(s);
}
