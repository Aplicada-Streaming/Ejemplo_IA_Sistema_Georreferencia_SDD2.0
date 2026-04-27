namespace Sgr.Frontend.Mobile.Geolocation;

internal static class Distance
{
    private const double EarthRadiusMeters = 6_371_000.0;

    /// <summary>
    /// Distancia ortodrómica entre dos pares lat/lng en metros.
    /// Suficientemente preciso para radios de descarte de pocas decenas de metros
    /// donde la curvatura terrestre es despreciable, y mucho más rápido que Vincenty.
    /// </summary>
    public static double HaversineMeters(
        double lat1, double lon1,
        double lat2, double lon2)
    {
        var phi1 = ToRadians(lat1);
        var phi2 = ToRadians(lat2);
        var dPhi = ToRadians(lat2 - lat1);
        var dLambda = ToRadians(lon2 - lon1);

        var a = Math.Sin(dPhi / 2) * Math.Sin(dPhi / 2) +
                Math.Cos(phi1) * Math.Cos(phi2) *
                Math.Sin(dLambda / 2) * Math.Sin(dLambda / 2);
        var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
        return EarthRadiusMeters * c;
    }

    private static double ToRadians(double deg) => deg * Math.PI / 180.0;
}
