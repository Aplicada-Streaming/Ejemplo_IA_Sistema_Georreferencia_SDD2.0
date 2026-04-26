using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Sgr.Persistence.DesignTime;

/// <summary>
/// Used by `dotnet ef` at design time so it does not need to bootstrap the API host.
/// The connection string here is only used to scaffold migrations; it is overridden
/// at runtime by the configuration in Sgr.Backend.Api.
/// </summary>
public sealed class SgrDbContextFactory : IDesignTimeDbContextFactory<SgrDbContext>
{
    public SgrDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<SgrDbContext>()
            .UseSqlServer(
                "Server=(localdb)\\MSSQLLocalDB;Database=SgrDev;Trusted_Connection=True;TrustServerCertificate=True",
                sql => sql.MigrationsAssembly(typeof(SgrDbContext).Assembly.FullName))
            .Options;
        return new SgrDbContext(options);
    }
}
