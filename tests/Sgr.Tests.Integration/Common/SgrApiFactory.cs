using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
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
            // Replace the SqlServer DbContext registration with an in-memory one.
            var descriptor = services.SingleOrDefault(d => d.ServiceType == typeof(DbContextOptions<SgrDbContext>));
            if (descriptor is not null) services.Remove(descriptor);

            services.AddDbContext<SgrDbContext>(opt => opt.UseInMemoryDatabase(_databaseName));
        });
    }
}
