using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Sgr.Frontend.Web.Api;

namespace Sgr.Frontend.Web.Auth;

public static class AuthEndpoints
{
    public static void MapAuthEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapPost("/auth/login", async (
            [FromForm] string email,
            [FromForm] string password,
            [FromForm] string? returnUrl,
            HttpContext ctx,
            ISgrApiClient api,
            CancellationToken ct) =>
        {
            var safeReturn = string.IsNullOrWhiteSpace(returnUrl) || !Uri.IsWellFormedUriString(returnUrl, UriKind.Relative)
                ? "/"
                : returnUrl;

            var result = await api.LoginAsync(email, password, ct);

            string ToLogin(string err) =>
                $"/login?error={Uri.EscapeDataString(err)}&returnUrl={Uri.EscapeDataString(safeReturn)}";

            switch (result)
            {
                case LoginResult.InvalidCredentials:
                    return Results.Redirect(ToLogin("El usuario o la contraseña son incorrectos."));
                case LoginResult.Forbidden f:
                    return Results.Redirect(ToLogin(f.Detail));
                case LoginResult.Error e:
                    return Results.Redirect(ToLogin(e.Detail));
                case LoginResult.Ok ok:
                    var claims = new List<Claim>
                    {
                        new(ClaimTypes.NameIdentifier, ok.Response.UserId.ToString()),
                        new(ClaimTypes.Email, email),
                        new(ClaimTypes.Name, email),
                        new(ClaimTypes.Role, ok.Response.Role),
                        new(SgrClaims.AccessToken, ok.Response.AccessToken),
                    };
                    if (ok.Response.AreaId is not null)
                        claims.Add(new Claim(SgrClaims.AreaId, ok.Response.AreaId.Value.ToString()));

                    var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
                    var principal = new ClaimsPrincipal(identity);

                    await ctx.SignInAsync(
                        CookieAuthenticationDefaults.AuthenticationScheme,
                        principal,
                        new AuthenticationProperties
                        {
                            IsPersistent = false,
                            ExpiresUtc = ok.Response.ExpiresAtUtc,
                        });

                    return Results.Redirect(safeReturn);
                default:
                    return Results.Redirect(ToLogin("Error desconocido."));
            }
        }).DisableAntiforgery().AllowAnonymous();

        app.MapPost("/auth/logout", async (HttpContext ctx) =>
        {
            await ctx.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return Results.Redirect("/login");
        }).DisableAntiforgery().AllowAnonymous();
    }
}
