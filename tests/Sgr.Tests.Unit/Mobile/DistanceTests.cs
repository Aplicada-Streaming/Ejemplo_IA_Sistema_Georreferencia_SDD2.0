using FluentAssertions;
using Sgr.Frontend.Mobile.Geolocation;

namespace Sgr.Tests.Unit.Mobile;

public class DistanceTests
{
    /// <summary>
    /// Mismo punto = distancia 0.
    /// </summary>
    [Fact]
    public void Same_point_is_zero_distance()
    {
        var d = Distance.HaversineMeters(-31.7496, -60.5213, -31.7496, -60.5213);
        d.Should().BeApproximately(0.0, 0.001);
    }

    /// <summary>
    /// Distancia conocida: 0.001° de latitud ≈ 111 metros (cualquier longitud).
    /// </summary>
    [Fact]
    public void Latitude_delta_001deg_is_approximately_111m()
    {
        var d = Distance.HaversineMeters(-31.7496, -60.5213, -31.7506, -60.5213);
        d.Should().BeApproximately(111.0, 1.0);
    }

    /// <summary>
    /// Distancia conocida: 0.001° de longitud a -31° latitud ≈ 95 m
    /// (varía con cos(lat) — en el ecuador serían 111 m).
    /// </summary>
    [Fact]
    public void Longitude_delta_001deg_at_31S_is_approximately_95m()
    {
        var d = Distance.HaversineMeters(-31.7496, -60.5213, -31.7496, -60.5223);
        d.Should().BeApproximately(94.6, 1.0);
    }

    /// <summary>
    /// Es simétrica: A→B == B→A.
    /// </summary>
    [Fact]
    public void Is_symmetric()
    {
        var a = Distance.HaversineMeters(-31.7, -60.5, -31.8, -60.6);
        var b = Distance.HaversineMeters(-31.8, -60.6, -31.7, -60.5);
        a.Should().BeApproximately(b, 0.0001);
    }

    /// <summary>
    /// Antípodas (puntos opuestos del globo): ≈ media circunferencia ≈ 20015 km.
    /// </summary>
    [Fact]
    public void Antipodal_is_half_circumference()
    {
        var d = Distance.HaversineMeters(0, 0, 0, 180);
        d.Should().BeApproximately(20_015_087.0, 100.0);   // ~20015 km
    }

    /// <summary>
    /// Comparación contra una distancia documentada conocida:
    /// Buenos Aires (-34.6037, -58.3816) ↔ Rosario (-32.9468, -60.6393) ≈ 280 km.
    /// </summary>
    [Fact]
    public void Buenos_Aires_to_Rosario_is_about_280km()
    {
        var d = Distance.HaversineMeters(-34.6037, -58.3816, -32.9468, -60.6393);
        d.Should().BeInRange(278_000, 282_000);
    }

    /// <summary>
    /// Para radios pequeños (típicos del descarte por cercanía en modo móvil),
    /// el resultado debe estar dentro de ±1m del esperado.
    /// </summary>
    [Theory]
    [InlineData(-31.7496, -60.5213, -31.7496, -60.5214, 9.5)]   // ~10m de longitud
    [InlineData(-31.7496, -60.5213, -31.7497, -60.5213, 11.1)]  // ~11m de latitud
    public void Small_distances_within_1m_tolerance(
        double lat1, double lon1, double lat2, double lon2, double expectedM)
    {
        var d = Distance.HaversineMeters(lat1, lon1, lat2, lon2);
        d.Should().BeApproximately(expectedM, 1.0);
    }
}
