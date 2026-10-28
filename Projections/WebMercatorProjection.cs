using Ape.Module.Geo;
using Ape.Module.Geo.Transforms;

namespace Ape.Module.Geo.Projections;

/// <summary>
/// Spherical Web Mercator (EPSG:3857). The sphere radius is the WGS-84 semi-major axis.
/// Height is copied through and is not part of the projection.
/// </summary>
public static class WebMercatorProjection
{
    public const double EarthRadiusMeters = Wgs84Ellipsoid.SemiMajorAxisMeters;

    public static double MaxLatitudeDeg { get; } = GeoMath.ToDegrees(Math.Atan(Math.Sinh(Math.PI)));

    public static ProjectedPoint FromLla(Lla lla)
    {
        GeoMath.RequireLla(lla, nameof(lla));
        if (Math.Abs(lla.LatitudeDeg) > MaxLatitudeDeg)
            throw new ArgumentOutOfRangeException(nameof(lla), lla.LatitudeDeg, "Latitude is outside the Web Mercator domain.");

        double lat = GeoMath.ToRadians(lla.LatitudeDeg);
        double lon = GeoMath.ToRadians(GeoMath.NormalizeLongitude(lla.LongitudeDeg));
        double easting = EarthRadiusMeters * lon;
        double northing = EarthRadiusMeters * Math.Log(Math.Tan(Math.PI / 4.0 + lat / 2.0));
        return new ProjectedPoint(easting, northing, lla.HeightMeters);
    }

    public static Lla ToLla(ProjectedPoint point)
    {
        GeoMath.RequireFinite(point.Easting, nameof(point));
        GeoMath.RequireFinite(point.Northing, nameof(point));
        GeoMath.RequireFinite(point.HeightMeters, nameof(point));

        double lon = point.Easting / EarthRadiusMeters;
        double lat = 2.0 * Math.Atan(Math.Exp(point.Northing / EarthRadiusMeters)) - Math.PI / 2.0;
        return new Lla(
            GeoMath.ToDegrees(lat),
            GeoMath.NormalizeLongitude(GeoMath.ToDegrees(lon)),
            point.HeightMeters);
    }
}
