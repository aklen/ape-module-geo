using Ape.Module.Geo;
using Ape.Module.Geo.Transforms;

namespace Ape.Module.Geo.Projections;

/// <summary>UPS coordinate: easting, northing, and ellipsoidal height, plus which polar zone.</summary>
public readonly record struct UpsCoordinate(
    double Easting,
    double Northing,
    double HeightMeters,
    bool IsNorthernHemisphere);

/// <summary>
/// WGS-84 Universal Polar Stereographic. The north zone is used north of 84°N and the south zone south of 80°S.
/// Height is copied through.
/// </summary>
public static class UpsProjection
{
    public const double ScaleFactor = 0.994;
    public const double FalseOffsetMeters = 2_000_000.0;

    private static readonly double Eccentricity = Math.Sqrt(Wgs84Ellipsoid.EccentricitySquared);
    private static readonly double RadiusFactor = Math.Sqrt(
        Math.Pow(1.0 + Eccentricity, 1.0 + Eccentricity) * Math.Pow(1.0 - Eccentricity, 1.0 - Eccentricity));

    public static bool Covers(double latitudeDeg) => latitudeDeg > 84.0 || latitudeDeg < -80.0;

    public static UpsCoordinate FromLla(Lla lla)
    {
        GeoMath.RequireLla(lla, nameof(lla));
        if (!Covers(lla.LatitudeDeg))
            throw new ArgumentOutOfRangeException(nameof(lla), lla.LatitudeDeg, "UPS covers latitudes south of 80°S and north of 84°N.");

        bool northern = lla.LatitudeDeg > 0.0;
        double rho = PolarRadius(Math.Abs(lla.LatitudeDeg));
        double lon = GeoMath.ToRadians(GeoMath.NormalizeLongitude(lla.LongitudeDeg));
        double easting = FalseOffsetMeters + rho * Math.Sin(lon);
        double northing = northern
            ? FalseOffsetMeters - rho * Math.Cos(lon)
            : FalseOffsetMeters + rho * Math.Cos(lon);

        return new UpsCoordinate(easting, northing, lla.HeightMeters, northern);
    }

    public static Lla ToLla(UpsCoordinate ups)
    {
        GeoMath.RequireFinite(ups.Easting, nameof(ups));
        GeoMath.RequireFinite(ups.Northing, nameof(ups));
        GeoMath.RequireFinite(ups.HeightMeters, nameof(ups));

        double dE = ups.Easting - FalseOffsetMeters;
        double dN = ups.Northing - FalseOffsetMeters;
        double rho = Math.Sqrt(dE * dE + dN * dN);
        if (rho < 1e-8)
        {
            return new Lla(ups.IsNorthernHemisphere ? 90.0 : -90.0, 0.0, ups.HeightMeters);
        }

        double lon = ups.IsNorthernHemisphere ? Math.Atan2(dE, -dN) : Math.Atan2(dE, dN);
        double t = rho * RadiusFactor / (2.0 * Wgs84Ellipsoid.SemiMajorAxisMeters * ScaleFactor);
        double lat = LatitudeFromConformal(t);
        if (!ups.IsNorthernHemisphere)
            lat = -lat;

        return new Lla(GeoMath.ToDegrees(lat), GeoMath.NormalizeLongitude(GeoMath.ToDegrees(lon)), ups.HeightMeters);
    }

    private static double PolarRadius(double absoluteLatitudeDeg)
    {
        double phi = GeoMath.ToRadians(absoluteLatitudeDeg);
        double sinPhi = Math.Sin(phi);
        double t = Math.Tan(Math.PI / 4.0 - phi / 2.0)
            / Math.Pow((1.0 - Eccentricity * sinPhi) / (1.0 + Eccentricity * sinPhi), Eccentricity / 2.0);
        return 2.0 * Wgs84Ellipsoid.SemiMajorAxisMeters * ScaleFactor * t / RadiusFactor;
    }

    private static double LatitudeFromConformal(double t)
    {
        double phi = Math.PI / 2.0 - 2.0 * Math.Atan(t);
        for (int i = 0; i < 12; i++)
        {
            double sinPhi = Math.Sin(phi);
            double next = Math.PI / 2.0 - 2.0 * Math.Atan(
                t * Math.Pow((1.0 - Eccentricity * sinPhi) / (1.0 + Eccentricity * sinPhi), Eccentricity / 2.0));
            if (Math.Abs(next - phi) < 1e-15)
                return next;

            phi = next;
        }

        return phi;
    }
}
