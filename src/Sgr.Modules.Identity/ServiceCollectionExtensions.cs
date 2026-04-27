using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Sgr.Modules.Identity.Application;
using Sgr.Modules.Identity.Authentication;

namespace Sgr.Modules.Identity;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddSgrIdentity(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.Configure<JwtOptions>(configuration.GetSection(JwtOptions.SectionName));
        services.AddSingleton<IPasswordHasher, Pbkdf2PasswordHasher>();
        services.AddSingleton<IJwtTokenService, JwtTokenService>();
        services.AddScoped<ILoginService, LoginService>();
        services.AddScoped<IUserRegistrationService, UserRegistrationService>();
        services.AddScoped<IUserManagementService, UserManagementService>();
        services.AddScoped<IAreaQueryService, AreaQueryService>();
        return services;
    }
}
