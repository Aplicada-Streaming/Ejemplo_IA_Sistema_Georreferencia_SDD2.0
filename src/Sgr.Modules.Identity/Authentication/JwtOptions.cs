namespace Sgr.Modules.Identity.Authentication;

public sealed class JwtOptions
{
    public const string SectionName = "Jwt";

    public string Issuer { get; set; } = "sgr-backend";
    public string Audience { get; set; } = "sgr-clients";
    public string SigningKey { get; set; } = default!;
    public int AccessTokenMinutes { get; set; } = 30;
}
