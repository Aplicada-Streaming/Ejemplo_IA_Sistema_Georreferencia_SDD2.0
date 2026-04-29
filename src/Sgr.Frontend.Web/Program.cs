using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.WebUtilities;
using MudBlazor.Services;
using Sgr.Frontend.Web.Api;
using Sgr.Frontend.Web.Auth;
using Sgr.Frontend.Web.Components;

var builder = WebApplication.CreateBuilder(args);

builder.Services.Configure<BackendApiOptions>(
    builder.Configuration.GetSection(BackendApiOptions.SectionName));

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddMudServices();
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<IApiTokenAccessor, HttpContextApiTokenAccessor>();

builder.Services.AddHttpClient<ISgrApiClient, SgrApiClient>((sp, http) =>
{
    var options = sp.GetRequiredService<Microsoft.Extensions.Options.IOptions<BackendApiOptions>>().Value;
    http.BaseAddress = new Uri(options.BaseUrl);
});

builder.Services.AddCascadingAuthenticationState();
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/login";
        options.LogoutPath = "/auth/logout";
        // AccessDeniedPath es PathString y NO admite query string: si le pasamos
        // "/login?error=acceso-denegado", el `?` se escapa a `%3F` y luego ASP.NET
        // appendea su propio `?ReturnUrl=...`, produciendo
        // /login%3Ferror=acceso-denegado?ReturnUrl=... que no matchea ninguna ruta
        // y devuelve 404. Armamos la URL completa en el evento.
        options.AccessDeniedPath = "/login";
        options.Cookie.Name = "sgr.web.auth";
        options.Cookie.HttpOnly = true;
        options.Cookie.SameSite = SameSiteMode.Lax;
        options.SlidingExpiration = false;
        options.Events.OnRedirectToAccessDenied = context =>
        {
            var returnUrl = context.Properties.RedirectUri
                ?? (context.Request.PathBase + context.Request.Path + context.Request.QueryString);
            var url = QueryHelpers.AddQueryString("/login", new Dictionary<string, string?>
            {
                ["error"] = "acceso-denegado",
                ["ReturnUrl"] = returnUrl,
            });
            context.Response.Redirect(url);
            return Task.CompletedTask;
        };
    });

builder.Services.AddAuthorization();
// Cada página declara su propia [Authorize]/[AllowAnonymous]; no usamos FallbackPolicy
// global porque interfiere con los endpoints de MapStaticAssets en .NET 10
// (los `_content/<RCL>/` quedan detrás del login y rompen el CSS).

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
}

app.UseAuthentication();
app.UseAuthorization();
app.UseAntiforgery();

app.MapStaticAssets();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.MapAuthEndpoints();

app.Run();
