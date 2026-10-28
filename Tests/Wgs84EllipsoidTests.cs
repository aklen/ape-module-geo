using Ape.Module.Geo.Transforms;

namespace Ape.Module.Geo.Tests;

public class Wgs84EllipsoidTests
{
    private const double SemiMinor = 6356752.314245179;

    [Fact]
    public void Equator_and_poles_match_the_axes()
    {
        Ecef equator = Wgs84Ellipsoid.ToEcef(new Lla(0, 0, 0));
        GeoAssert.Meters(Wgs84Ellipsoid.SemiMajorAxisMeters, equator.X, 1e-6);
        GeoAssert.Meters(0, equator.Y, 1e-6);
        GeoAssert.Meters(0, equator.Z, 1e-6);

        Ecef east = Wgs84Ellipsoid.ToEcef(new Lla(0, 90, 0));
        GeoAssert.Meters(0, east.X, 1e-6);
        GeoAssert.Meters(Wgs84Ellipsoid.SemiMajorAxisMeters, east.Y, 1e-6);
        GeoAssert.Meters(0, east.Z, 1e-6);

        Ecef north = Wgs84Ellipsoid.ToEcef(new Lla(90, 0, 0));
        GeoAssert.Meters(0, north.X, 1e-6);
        GeoAssert.Meters(0, north.Y, 1e-6);
        GeoAssert.Meters(SemiMinor, north.Z, 1e-6);

        Ecef south = Wgs84Ellipsoid.ToEcef(new Lla(-90, 0, 0));
        GeoAssert.Meters(-SemiMinor, south.Z, 1e-6);
    }

    [Fact]
    public void Known_point_matches_ecef()
    {
        Ecef ecef = Wgs84Ellipsoid.ToEcef(new Lla(10, 50, 100));
        GeoAssert.Meters(4037973.322717777, ecef.X, 1e-4);
        GeoAssert.Meters(4812269.214148490, ecef.Y, 1e-4);
        GeoAssert.Meters(1100265.912553128, ecef.Z, 1e-4);
    }

    [Fact]
    public void Roundtrip_returns_the_same_lla()
    {
        var source = new Lla(-45, 170, 50);
        Lla back = Wgs84Ellipsoid.ToLla(Wgs84Ellipsoid.ToEcef(source));
        GeoAssert.Degrees(-45, back.LatitudeDeg);
        GeoAssert.Degrees(170, back.LongitudeDeg);
        GeoAssert.Meters(50, back.HeightMeters, 1e-4);
    }
}
