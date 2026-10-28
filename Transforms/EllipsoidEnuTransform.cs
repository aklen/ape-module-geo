using System.Numerics;
using Ape.Module.Geo;

namespace Ape.Module.Geo.Transforms;

/// <summary>
/// Ellipsoidal WGS-84 LLA to local ENU. The math is double precision; <see cref="Vector3"/> rounds the result.
/// </summary>
/// <remarks>
/// Latitude, longitude, and ellipsoidal height use the same <see cref="Vector3"/> slots as <see cref="Wgs84Transform"/>:
/// X latitude degrees, Y longitude degrees, Z height meters. Internal X, Y, Z are east, north, up meters.
/// </remarks>
public sealed class EllipsoidEnuTransform : ICoordinateTransform
{
    private readonly Lla _origin;

    public EllipsoidEnuTransform(double originLatitude = 0.0, double originLongitude = 0.0, double originAltitude = 0.0)
    {
        _origin = new Lla(originLatitude, originLongitude, originAltitude);
        GeoMath.RequireLla(_origin, nameof(originLatitude));
    }

    public string CoordinateSystemName => "WGS-84-Ellipsoid";

    public Vector3 ToInternal(Vector3 lla)
    {
        Enu enu = TangentFrame.LlaToEnu(new Lla(lla.X, lla.Y, lla.Z), _origin);
        return new Vector3((float)enu.East, (float)enu.North, (float)enu.Up);
    }

    public Vector3 FromInternal(Vector3 enu)
    {
        Lla lla = TangentFrame.EnuToLla(new Enu(enu.X, enu.Y, enu.Z), _origin);
        return new Vector3((float)lla.LatitudeDeg, (float)lla.LongitudeDeg, (float)lla.HeightMeters);
    }
}
