using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Sgr.Domain.Common;

namespace Sgr.Persistence;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddSgrPersistence(
        this IServiceCollection services,
        string connectionString)
    {
        services.AddDbContext<SgrDbContext>(options =>
            options.UseSqlServer(connectionString, sql =>
                sql.MigrationsAssembly(typeof(SgrDbContext).Assembly.FullName)));

        services.AddSingleton<IDateTimeProvider, SystemDateTimeProvider>();

        return services;
    }
}
