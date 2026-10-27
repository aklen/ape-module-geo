using System.Numerics;

namespace Ape.Module.Geo.Transforms;

/// <summary>
/// WGS-84 coordinate transformer (latitude, longitude, altitude in degrees and meters).
/// Internal frame is local ENU (east-north-up) relative to the configured origin.
/// </summary>
public class Wgs84Transform : ICoordinateTransform
{
    private readonly Vector3 _originLla;
    private const double EarthRadiusMeters = 6371000.0;

    public string CoordinateSystemName => "WGS-84";

    public Wgs84Transform(double originLatitude = 0.0, double originLongitude = 0.0, double originAltitude = 0.0)
    {
        _originLla = new Vector3((float)originLatitude, (float)originLongitude, (float)originAltitude);
    }

    /// <summary>Convert WGS-84 (lat, lon, alt) to local ENU cartesian.</summary>
    public Vector3 ToInternal(Vector3 lla)
    {
        double lat = lla.X * Math.PI / 180.0;
        double lon = lla.Y * Math.PI / 180.0;
        double alt = lla.Z;

        double originLat = _originLla.X * Math.PI / 180.0;
        double originLon = _originLla.Y * Math.PI / 180.0;
        double originAlt = _originLla.Z;

        double dLat = lat - originLat;
        double dLon = lon - originLon;
        double dAlt = alt - originAlt;

        double east = dLon * EarthRadiusMeters * Math.Cos(originLat);
        double north = dLat * EarthRadiusMeters;
        double up = dAlt;

        return new Vector3((float)east, (float)north, (float)up);
    }

    /// <summary>Convert local ENU cartesian back to WGS-84.</summary>
    public Vector3 FromInternal(Vector3 enu)
    {
        double east = enu.X;
        double north = enu.Y;
        double up = enu.Z;

        double originLat = _originLla.X * Math.PI / 180.0;
        double originLon = _originLla.Y * Math.PI / 180.0;
        double originAlt = _originLla.Z;

        double dLat = north / EarthRadiusMeters;
        double dLon = east / (EarthRadiusMeters * Math.Cos(originLat));

        double lat = (originLat + dLat) * 180.0 / Math.PI;
        double lon = (originLon + dLon) * 180.0 / Math.PI;
        double alt = originAlt + up;

        return new Vector3((float)lat, (float)lon, (float)alt);
    }
}

/// <summary>Local Cartesian coordinate system (X, Y, Z in meters); direct passthrough.</summary>
public class LocalCartesianTransform : ICoordinateTransform
{
    public string CoordinateSystemName => "Local-Cartesian";

    public Vector3 ToInternal(Vector3 sourceCoords) => sourceCoords;

    public Vector3 FromInternal(Vector3 internalCoords) => internalCoords;
}

/// <summary>Unity3D coordinate system transformer (left-handed Y-up) from ENU (right-handed Z-up).</summary>
public class UnityTransform : ICoordinateTransform
{
    public string CoordinateSystemName => "Unity3D";

    public Vector3 ToInternal(Vector3 unityCoords) =>
        new(unityCoords.X, unityCoords.Z, unityCoords.Y);

    public Vector3 FromInternal(Vector3 enuCoords) =>
        new(enuCoords.X, enuCoords.Z, enuCoords.Y);
}
