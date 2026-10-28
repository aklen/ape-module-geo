using System.Numerics;
using Ape.Module.Geo.Transforms;

namespace Ape.Module.Geo.Tests;

public class TangentFrameTests
{
    private static readonly Lla Origin = new(47.37, 8.4, 10);

    [Fact]
    public void Origin_is_zero_enu_and_ned()
    {
        Enu enu = TangentFrame.LlaToEnu(Origin, Origin);
        Ned ned = TangentFrame.LlaToNed(Origin, Origin);
        GeoAssert.Meters(0, enu.East, 1e-6);
        GeoAssert.Meters(0, enu.North, 1e-6);
        GeoAssert.Meters(0, enu.Up, 1e-6);
        GeoAssert.Meters(0, ned.North, 1e-6);
        GeoAssert.Meters(0, ned.East, 1e-6);
        GeoAssert.Meters(0, ned.Down, 1e-6);
    }

    [Theory]
    [InlineData(47.38, 8.41, 30, 755.154343, 1111.835247, 19.858347)]
    [InlineData(47.36, 8.39, 0, -755.436328, -1111.731073, -10.141669)]
    public void Lla_matches_local_enu(double lat, double lon, double height, double east, double north, double up)
    {
        Enu enu = TangentFrame.LlaToEnu(new Lla(lat, lon, height), Origin);
        GeoAssert.Meters(east, enu.East, 1e-3);
        GeoAssert.Meters(north, enu.North, 1e-3);
        GeoAssert.Meters(up, enu.Up, 1e-3);

        Lla back = TangentFrame.EnuToLla(enu, Origin);
        GeoAssert.Degrees(lat, back.LatitudeDeg, 1e-8);
        GeoAssert.Degrees(lon, back.LongitudeDeg, 1e-8);
        GeoAssert.Meters(height, back.HeightMeters, 1e-4);
    }

    [Fact]
    public void Ned_swaps_and_flips_enu()
    {
        var point = new Lla(47.38, 8.41, 30);
        Enu enu = TangentFrame.LlaToEnu(point, Origin);
        Ned ned = TangentFrame.LlaToNed(point, Origin);
        GeoAssert.Meters(enu.North, ned.North, 1e-9);
        GeoAssert.Meters(enu.East, ned.East, 1e-9);
        GeoAssert.Meters(-enu.Up, ned.Down, 1e-9);
        Lla back = TangentFrame.NedToLla(ned, Origin);
        GeoAssert.Degrees(point.LatitudeDeg, back.LatitudeDeg, 1e-8);
        GeoAssert.Degrees(point.LongitudeDeg, back.LongitudeDeg, 1e-8);
    }
}

public class EllipsoidEnuTransformTests
{
    [Fact]
    public void Vector_adapter_follows_the_double_path()
    {
        var origin = new Lla(47.37, 8.4, 10);
        var transform = new EllipsoidEnuTransform(origin.LatitudeDeg, origin.LongitudeDeg, origin.HeightMeters);
        Assert.Equal("WGS-84-Ellipsoid", transform.CoordinateSystemName);

        var input = new Vector3(47.38f, 8.41f, 30f);
        Vector3 enu = transform.ToInternal(input);
        Enu precise = TangentFrame.LlaToEnu(new Lla(input.X, input.Y, input.Z), origin);
        GeoAssert.Meters(precise.East, enu.X, 0.01);
        GeoAssert.Meters(precise.North, enu.Y, 0.01);
        GeoAssert.Meters(precise.Up, enu.Z, 0.01);
    }
}

public class SphericalWgs84Tests
{
    [Fact]
    public void One_degree_east_uses_the_sphere()
    {
        var transform = new Wgs84Transform();
        Vector3 enu = transform.ToInternal(new Vector3(0f, 1f, 0f));
        double expectedEast = 6371000.0 * Math.PI / 180.0;
        GeoAssert.Meters(expectedEast, enu.X, 1e-2);
        GeoAssert.Meters(0, enu.Y, 1e-2);
        GeoAssert.Meters(0, enu.Z, 1e-2);
    }
}
