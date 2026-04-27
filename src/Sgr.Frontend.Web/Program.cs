using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
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
        options.AccessDeniedPath = "/login?error=acceso-denegado";
        options.Cookie.Name = "sgr.web.auth";
        options.Cookie.HttpOnly = true;
        options.Cookie.SameSite = SameSiteMode.Lax;
        options.SlidingExpiration = false;
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

// MapStaticAssets sirve wwwroot + `_content/<RCL>/` (MudBlazor.min.css, etc).
// Va antes de auth para que CSS/JS estén accesibles también desde la página de login.
app.MapStaticAssets();

app.UseAuthentication();
app.UseAuthorization();
app.UseAntiforgery();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode()
    .WithStaticAssets();

app.MapAuthEndpoints();

app.Run();
