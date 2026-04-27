using Microsoft.EntityFrameworkCore;
using Sgr.Domain.Common;
using Sgr.Persistence;

namespace Sgr.Tests.Unit.Common;

/// <summary>
/// Helpers comunes para tests de application services con InMemory DB.
/// Cada test pide un contexto fresh (Guid único en el name del provider) para
/// aislar estado entre tests sin tener que limpiar manualmente.
/// </summary>
internal static class TestDb
{
    public static SgrDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<SgrDbContext>()
            .UseInMemoryDatabase($"sgr-unit-{Guid.NewGuid():N}")
            // InMemory no enforece check constraints; nuestros tests validan eso
            // a nivel domain (entity factories), así que está bien para application services.
            .ConfigureWarnings(w => w.Ignore(
                Microsoft.EntityFrameworkCore.Diagnostics.InMemoryEventId.TransactionIgnoredWarning))
            .Options;
        return new SgrDbContext(options);
    }
}

/// <summary>
/// Reloj fijo para tests deterministas. Permite avanzar tiempo manualmente.
/// </summary>
internal sealed class FakeDateTimeProvider : IDateTimeProvider
{
    private DateTime _now;
    public FakeDateTimeProvider(DateTime initial) => _now = DateTime.SpecifyKind(initial, DateTimeKind.Utc);
    public DateTime UtcNow => _now;
    public void Advance(TimeSpan delta) => _now = _now.Add(delta);
    public void SetTo(DateTime utc) => _now = DateTime.SpecifyKind(utc, DateTimeKind.Utc);
}
