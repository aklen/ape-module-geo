using Ape.Module.Geo;
using Ape.Module.Geo.Transforms;

namespace Ape.Module.Geo.Projections;

/// <summary>UTM coordinate: easting, northing, and ellipsoidal height, plus zone and hemisphere.</summary>
public readonly record struct UtmCoordinate(
    double Easting,
    double Northing,
    double HeightMeters,
    int Zone,
    bool IsNorthernHemisphere);

/// <summary>
/// WGS-84 Universal Transverse Mercator. Zones run from 80°S through 84°N.
/// Norway and Svalbard use the widened zones. Height is copied through.
/// </summary>
public static class UtmProjection
{
    public const double FalseEastingMeters = 500_000.0;
    public const double FalseNorthingMeters = 10_000_000.0;
    public const double ScaleFactor = 0.9996;

    public static bool Covers(double latitudeDeg) => latitudeDeg is >= -80.0 and <= 84.0;

    public static int ZoneFor(double latitudeDeg, double longitudeDeg)
    {
        GeoMath.RequireLatitude(latitudeDeg, nameof(latitudeDeg));
        GeoMath.RequireFinite(longitudeDeg, nameof(longitudeDeg));
        if (!Covers(latitudeDeg))
            throw new ArgumentOutOfRangeException(nameof(latitudeDeg), latitudeDeg, "Latitude is outside the UTM domain, from 80°S through 84°N.");

        double lon = GeoMath.NormalizeLongitude(longitudeDeg);

        if (latitudeDeg is >= 72.0 and <= 84.0)
        {
            if (lon is >= 0.0 and < 9.0)
                return 31;
            if (lon is >= 9.0 and < 21.0)
                return 33;
            if (lon is >= 21.0 and < 33.0)
                return 35;
            if (lon is >= 33.0 and < 42.0)
                return 37;
        }

        if (latitudeDeg is >= 56.0 and < 64.0 && lon is >= 3.0 and < 12.0)
            return 32;

        int zone = (int)Math.Floor((lon + 180.0) / 6.0) + 1;
        if (zone < 1)
            return 1;
        if (zone > 60)
            return 60;

        return zone;
    }

    public static double CentralMeridianDeg(int zone)
    {
        RequireZone(zone);
        return zone * 6.0 - 183.0;
    }

    public static UtmCoordinate FromLla(Lla lla) => FromLla(lla, ZoneFor(lla.LatitudeDeg, lla.LongitudeDeg));

    public static UtmCoordinate FromLla(Lla lla, int zone)
    {
        GeoMath.RequireLla(lla, nameof(lla));
        RequireZone(zone);
        if (!Covers(lla.LatitudeDeg))
            throw new ArgumentOutOfRangeException(nameof(lla), lla.LatitudeDeg, "Latitude is outside the UTM domain, from 80°S through 84°N.");

        double lon0 = GeoMath.ToRadians(CentralMeridianDeg(zone));
        (double east, double north) = TransverseMercator.Forward(
            GeoMath.ToRadians(lla.LatitudeDeg),
            GeoMath.ToRadians(lla.LongitudeDeg),
            lon0);

        bool northern = lla.LatitudeDeg >= 0.0;
        return new UtmCoordinate(
            east + FalseEastingMeters,
            northern ? north : north + FalseNorthingMeters,
            lla.HeightMeters,
            zone,
            northern);
    }

    public static Lla ToLla(UtmCoordinate utm)
    {
        RequireZone(utm.Zone);
        GeoMath.RequireFinite(utm.Easting, nameof(utm));
        GeoMath.RequireFinite(utm.Northing, nameof(utm));
        GeoMath.RequireFinite(utm.HeightMeters, nameof(utm));

        double east = utm.Easting - FalseEastingMeters;
        double north = utm.IsNorthernHemisphere ? utm.Northing : utm.Northing - FalseNorthingMeters;
        double lon0 = GeoMath.ToRadians(CentralMeridianDeg(utm.Zone));
        (double lat, double lon) = TransverseMercator.Inverse(east, north, lon0);
        return new Lla(GeoMath.ToDegrees(lat), GeoMath.NormalizeLongitude(GeoMath.ToDegrees(lon)), utm.HeightMeters);
    }

    private static void RequireZone(int zone)
    {
        if (zone is < 1 or > 60)
            throw new ArgumentOutOfRangeException(nameof(zone), zone, "UTM zone must be from 1 to 60.");
    }
}

/// <summary>
/// Krüger series for the transverse Mercator, truncated at n³.
/// That truncation is about a millimetre inside a UTM zone. Offsets are not applied here.
/// </summary>
internal static class TransverseMercator
{
    private static readonly double N = Wgs84Ellipsoid.Flattening / (2.0 - Wgs84Ellipsoid.Flattening);
    private static readonly double A = Wgs84Ellipsoid.SemiMajorAxisMeters / (1.0 + N) * (1.0 + N * N / 4.0 + Math.Pow(N, 4) / 64.0);
    private static readonly double Alpha1 = 0.5 * N - 2.0 / 3.0 * N * N + 5.0 / 16.0 * N * N * N;
    private static readonly double Alpha2 = 13.0 / 48.0 * N * N - 0.6 * N * N * N;
    private static readonly double Alpha3 = 61.0 / 240.0 * N * N * N;
    private static readonly double Beta1 = 0.5 * N - 2.0 / 3.0 * N * N + 37.0 / 96.0 * N * N * N;
    private static readonly double Beta2 = 1.0 / 48.0 * N * N + 1.0 / 15.0 * N * N * N;
    private static readonly double Beta3 = 17.0 / 480.0 * N * N * N;
    private static readonly double Delta1 = 2.0 * N - 2.0 / 3.0 * N * N - 2.0 * N * N * N;
    private static readonly double Delta2 = 7.0 / 3.0 * N * N - 1.6 * N * N * N;
    private static readonly double Delta3 = 56.0 / 15.0 * N * N * N;
    private static readonly double ConformalFactor = 2.0 * Math.Sqrt(N) / (1.0 + N);

    public static (double Easting, double Northing) Forward(double latRad, double lonRad, double lon0Rad)
    {
        double dLon = GeoMath.WrapLongitudeDeltaRadians(lonRad - lon0Rad);
        double sinLat = Math.Sin(latRad);
        double t = Math.Sinh(GeoMath.Atanh(sinLat) - ConformalFactor * GeoMath.Atanh(ConformalFactor * sinLat));
        double xi = Math.Atan2(t, Math.Cos(dLon));
        double eta = GeoMath.Atanh(Math.Sin(dLon) / Math.Sqrt(1.0 + t * t));

        double east = eta
            + Alpha1 * Math.Cos(2.0 * xi) * Math.Sinh(2.0 * eta)
            + Alpha2 * Math.Cos(4.0 * xi) * Math.Sinh(4.0 * eta)
            + Alpha3 * Math.Cos(6.0 * xi) * Math.Sinh(6.0 * eta);
        double north = xi
            + Alpha1 * Math.Sin(2.0 * xi) * Math.Cosh(2.0 * eta)
            + Alpha2 * Math.Sin(4.0 * xi) * Math.Cosh(4.0 * eta)
            + Alpha3 * Math.Sin(6.0 * xi) * Math.Cosh(6.0 * eta);

        return (UtmProjection.ScaleFactor * A * east, UtmProjection.ScaleFactor * A * north);
    }

    public static (double LatitudeRad, double LongitudeRad) Inverse(double easting, double northing, double lon0Rad)
    {
        double xi = northing / (UtmProjection.ScaleFactor * A);
        double eta = easting / (UtmProjection.ScaleFactor * A);
        double xiPrime = xi
            - Beta1 * Math.Sin(2.0 * xi) * Math.Cosh(2.0 * eta)
            - Beta2 * Math.Sin(4.0 * xi) * Math.Cosh(4.0 * eta)
            - Beta3 * Math.Sin(6.0 * xi) * Math.Cosh(6.0 * eta);
        double etaPrime = eta
            - Beta1 * Math.Cos(2.0 * xi) * Math.Sinh(2.0 * eta)
            - Beta2 * Math.Cos(4.0 * xi) * Math.Sinh(4.0 * eta)
            - Beta3 * Math.Cos(6.0 * xi) * Math.Sinh(6.0 * eta);

        double chi = Math.Asin(Math.Sin(xiPrime) / Math.Cosh(etaPrime));
        double lat = chi
            + Delta1 * Math.Sin(2.0 * chi)
            + Delta2 * Math.Sin(4.0 * chi)
            + Delta3 * Math.Sin(6.0 * chi);
        double lon = lon0Rad + Math.Atan2(Math.Sinh(etaPrime), Math.Cos(xiPrime));
        return (lat, lon);
    }
}
