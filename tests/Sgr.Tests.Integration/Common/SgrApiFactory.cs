using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Sgr.Persistence;

namespace Sgr.Tests.Integration.Common;

public class SgrApiFactory : WebApplicationFactory<Program>
{
    private readonly string _databaseName = $"sgr-test-{Guid.NewGuid():N}";

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");

        builder.ConfigureAppConfiguration((_, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:DefaultConnection"] = "InMemory",
                ["Jwt:SigningKey"] = "TEST_ONLY_SIGNING_KEY_AT_LEAST_32_CHARS_LONG_!_OK",
                ["Jwt:Issuer"] = "sgr-backend-test",
                ["Jwt:Audience"] = "sgr-clients-test",
                ["Jwt:AccessTokenMinutes"] = "30",
            });
        });

        builder.ConfigureServices(services =>
        {
            // EF Core 10 detecta múltiples providers cuando se reemplaza solo
            // DbContextOptions<T>. Hay que limpiar también los configuradores
            // (IDbContextOptionsConfiguration<T>) y volver a registrar con InMemory.
            services.RemoveAll(typeof(DbContextOptions<SgrDbContext>));
            services.RemoveAll(typeof(DbContextOptions));
            services.RemoveAll(typeof(IDbContextOptionsConfiguration<SgrDbContext>));

            services.AddDbContext<SgrDbContext>(opt => opt.UseInMemoryDatabase(_databaseName));
        });
    }
}
