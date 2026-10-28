using Ape.Module.Geo;

namespace Ape.Module.Geo.Transforms;

/// <summary>
/// Local tangent-plane frames at a geodetic origin. ENU is east, north, up. NED is north, east, down.
/// </summary>
public static class TangentFrame
{
    public static Enu EcefToEnu(Ecef point, Lla origin)
    {
        var delta = Delta(point, origin);
        Trig(origin, out double sinLat, out double cosLat, out double sinLon, out double cosLon);

        return new Enu(
            -sinLon * delta.X + cosLon * delta.Y,
            -sinLat * cosLon * delta.X - sinLat * sinLon * delta.Y + cosLat * delta.Z,
            cosLat * cosLon * delta.X + cosLat * sinLon * delta.Y + sinLat * delta.Z);
    }

    public static Ecef EnuToEcef(Enu point, Lla origin)
    {
        GeoMath.RequireFinite(point.East, nameof(point));
        GeoMath.RequireFinite(point.North, nameof(point));
        GeoMath.RequireFinite(point.Up, nameof(point));

        var originEcef = Wgs84Ellipsoid.ToEcef(origin);
        Trig(origin, out double sinLat, out double cosLat, out double sinLon, out double cosLon);

        double dx = -sinLon * point.East - sinLat * cosLon * point.North + cosLat * cosLon * point.Up;
        double dy = cosLon * point.East - sinLat * sinLon * point.North + cosLat * sinLon * point.Up;
        double dz = cosLat * point.North + sinLat * point.Up;
        return new Ecef(originEcef.X + dx, originEcef.Y + dy, originEcef.Z + dz);
    }

    public static Ned EcefToNed(Ecef point, Lla origin)
    {
        Enu enu = EcefToEnu(point, origin);
        return new Ned(enu.North, enu.East, -enu.Up);
    }

    public static Ecef NedToEcef(Ned point, Lla origin) =>
        EnuToEcef(new Enu(point.East, point.North, -point.Down), origin);

    public static Enu LlaToEnu(Lla point, Lla origin) =>
        EcefToEnu(Wgs84Ellipsoid.ToEcef(point), origin);

    public static Lla EnuToLla(Enu point, Lla origin) =>
        Wgs84Ellipsoid.ToLla(EnuToEcef(point, origin));

    public static Ned LlaToNed(Lla point, Lla origin) =>
        EcefToNed(Wgs84Ellipsoid.ToEcef(point), origin);

    public static Lla NedToLla(Ned point, Lla origin) =>
        Wgs84Ellipsoid.ToLla(NedToEcef(point, origin));

    private static Ecef Delta(Ecef point, Lla origin)
    {
        GeoMath.RequireFinite(point.X, nameof(point));
        GeoMath.RequireFinite(point.Y, nameof(point));
        GeoMath.RequireFinite(point.Z, nameof(point));

        Ecef originEcef = Wgs84Ellipsoid.ToEcef(origin);
        return new Ecef(point.X - originEcef.X, point.Y - originEcef.Y, point.Z - originEcef.Z);
    }

    private static void Trig(
        Lla origin,
        out double sinLat,
        out double cosLat,
        out double sinLon,
        out double cosLon)
    {
        GeoMath.RequireLla(origin, nameof(origin));
        double lat = GeoMath.ToRadians(origin.LatitudeDeg);
        double lon = GeoMath.ToRadians(origin.LongitudeDeg);
        sinLat = Math.Sin(lat);
        cosLat = Math.Cos(lat);
        sinLon = Math.Sin(lon);
        cosLon = Math.Cos(lon);
    }
}
