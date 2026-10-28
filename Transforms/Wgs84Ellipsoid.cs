using Ape.Module.Geo;

namespace Ape.Module.Geo.Transforms;

/// <summary>
/// WGS-84 ellipsoid and the conversion between geodetic LLA and ECEF.
/// </summary>
public static class Wgs84Ellipsoid
{
    public const double SemiMajorAxisMeters = 6378137.0;
    public const double InverseFlattening = 298.257223563;

    public static double Flattening { get; } = 1.0 / InverseFlattening;

    public static double SemiMinorAxisMeters { get; } = SemiMajorAxisMeters * (1.0 - Flattening);

    public static double EccentricitySquared { get; } = Flattening * (2.0 - Flattening);

    public static double SecondEccentricitySquared { get; } = EccentricitySquared / (1.0 - EccentricitySquared);

    public static Ecef ToEcef(Lla lla)
    {
        GeoMath.RequireLla(lla, nameof(lla));

        double lat = GeoMath.ToRadians(lla.LatitudeDeg);
        double lon = GeoMath.ToRadians(lla.LongitudeDeg);
        double sinLat = Math.Sin(lat);
        double cosLat = Math.Cos(lat);
        double n = PrimeVerticalRadius(sinLat);
        double height = lla.HeightMeters;

        return new Ecef(
            (n + height) * cosLat * Math.Cos(lon),
            (n + height) * cosLat * Math.Sin(lon),
            (n * (1.0 - EccentricitySquared) + height) * sinLat);
    }

    public static Lla ToLla(Ecef ecef)
    {
        GeoMath.RequireFinite(ecef.X, nameof(ecef));
        GeoMath.RequireFinite(ecef.Y, nameof(ecef));
        GeoMath.RequireFinite(ecef.Z, nameof(ecef));

        double x = ecef.X;
        double y = ecef.Y;
        double z = ecef.Z;
        double p = Math.Sqrt(x * x + y * y);
        double lon = Math.Atan2(y, x);
        double lat = BowringLatitude(p, z);

        for (int i = 0; i < 5; i++)
        {
            double sinLat = Math.Sin(lat);
            double n = PrimeVerticalRadius(sinLat);
            double height = Height(p, z, lat, n);
            double denominator = n + height;
            if (Math.Abs(denominator) < 1.0)
                break;

            double next = Math.Atan2(z, p * (1.0 - EccentricitySquared * n / denominator));
            if (Math.Abs(next - lat) < 1e-15)
            {
                lat = next;
                break;
            }

            lat = next;
        }

        double sinFinal = Math.Sin(lat);
        double nFinal = PrimeVerticalRadius(sinFinal);
        return new Lla(
            GeoMath.ToDegrees(lat),
            GeoMath.NormalizeLongitude(GeoMath.ToDegrees(lon)),
            Height(p, z, lat, nFinal));
    }

    internal static double PrimeVerticalRadius(double sinLat) =>
        SemiMajorAxisMeters / Math.Sqrt(1.0 - EccentricitySquared * sinLat * sinLat);

    private static double BowringLatitude(double p, double z)
    {
        double a = SemiMajorAxisMeters;
        double b = SemiMinorAxisMeters;
        double theta = Math.Atan2(z * a, p * b);
        double sinTheta = Math.Sin(theta);
        double cosTheta = Math.Cos(theta);
        return Math.Atan2(
            z + SecondEccentricitySquared * b * sinTheta * sinTheta * sinTheta,
            p - EccentricitySquared * a * cosTheta * cosTheta * cosTheta);
    }

    private static double Height(double p, double z, double lat, double primeVertical)
    {
        double cosLat = Math.Cos(lat);
        if (Math.Abs(cosLat) > 1e-12)
            return p / cosLat - primeVertical;

        double sinLat = Math.Sin(lat);
        return z / sinLat - primeVertical * (1.0 - EccentricitySquared);
    }
}
