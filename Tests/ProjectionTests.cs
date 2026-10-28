using Ape.Module.Geo.Projections;

namespace Ape.Module.Geo.Tests;

public class WebMercatorTests
{
    [Fact]
    public void Origin_is_zero_and_a_known_point_matches()
    {
        ProjectedPoint origin = WebMercatorProjection.FromLla(new Lla(0, 0, 12));
        GeoAssert.Meters(0, origin.Easting, 1e-6);
        GeoAssert.Meters(0, origin.Northing, 1e-6);
        GeoAssert.Meters(12, origin.HeightMeters, 1e-9);

        ProjectedPoint point = WebMercatorProjection.FromLla(new Lla(50, 10, 4));
        GeoAssert.Meters(1113194.907933, point.Easting, 1e-3);
        GeoAssert.Meters(6446275.841017, point.Northing, 1e-3);
        GeoAssert.Meters(4, point.HeightMeters, 1e-9);

        Lla back = WebMercatorProjection.ToLla(point);
        GeoAssert.Degrees(50, back.LatitudeDeg, 1e-9);
        GeoAssert.Degrees(10, back.LongitudeDeg, 1e-9);
        GeoAssert.Meters(4, back.HeightMeters, 1e-9);
    }

    [Fact]
    public void Latitude_past_the_limit_is_rejected()
    {
        double over = WebMercatorProjection.MaxLatitudeDeg + 0.001;
        Assert.Throws<ArgumentOutOfRangeException>(() => WebMercatorProjection.FromLla(new Lla(over, 0, 0)));
    }
}

public class UtmProjectionTests
{
    [Theory]
    [InlineData(47.37, 8.4, 32)]
    [InlineData(60, 2.9, 31)]
    [InlineData(60, 3, 32)]
    [InlineData(60, 12, 33)]
    [InlineData(55.9, 3, 31)]
    [InlineData(71.999, 6, 32)]
    [InlineData(72, 6, 31)]
    [InlineData(78, 8, 31)]
    [InlineData(78, 12, 33)]
    [InlineData(78, 20, 33)]
    [InlineData(78, 32, 35)]
    [InlineData(80, 40, 37)]
    [InlineData(0, 180, 60)]
    [InlineData(0, -180, 1)]
    [InlineData(-33.8688, 151.2093, 56)]
    public void Zone_follows_the_standard_and_the_widened_bands(double lat, double lon, int zone) =>
        Assert.Equal(zone, UtmProjection.ZoneFor(lat, lon));

    [Theory]
    [InlineData(47.37, 8.4, 32, true, 454700.5796, 5246456.7143)]
    [InlineData(60, 3, 32, true, 165640.3321, 6666593.5721)]
    [InlineData(60, 2.9, 31, true, 494422.2333, 6651415.4058)]
    [InlineData(78, 12, 33, true, 430399.6201, 8660152.3441)]
    [InlineData(78, 8, 31, true, 615914.5249, 8663320.2014)]
    [InlineData(80, 40, 37, true, 519384.8033, 8881752.4148)]
    [InlineData(-33.8688, 151.2093, 56, false, 334368.6336, 6250948.3454)]
    [InlineData(40.7128, -74.006, 18, true, 583959.3723, 4507350.9982)]
    public void Forward_matches_published_grid(
        double lat, double lon, int zone, bool northern, double easting, double northing)
    {
        UtmCoordinate utm = UtmProjection.FromLla(new Lla(lat, lon, 7));
        Assert.Equal(zone, utm.Zone);
        Assert.Equal(northern, utm.IsNorthernHemisphere);
        GeoAssert.Meters(7, utm.HeightMeters, 1e-9);
        GeoAssert.Meters(easting, utm.Easting, 1e-3);
        GeoAssert.Meters(northing, utm.Northing, 1e-3);

        Lla back = UtmProjection.ToLla(utm);
        GeoAssert.Degrees(lat, back.LatitudeDeg, 1e-7);
        GeoAssert.Degrees(lon, back.LongitudeDeg, 1e-7);
        GeoAssert.Meters(7, back.HeightMeters, 1e-9);
    }

    [Fact]
    public void Caller_can_force_a_neighboring_zone()
    {
        UtmCoordinate utm = UtmProjection.FromLla(new Lla(47.37, 8.4, 0), 33);
        Assert.Equal(33, utm.Zone);
        GeoAssert.Meters(1797.4513, utm.Easting, 1e-3);
        GeoAssert.Meters(5267438.9835, utm.Northing, 1e-3);
    }

    [Fact]
    public void Polar_latitude_is_rejected() =>
        Assert.Throws<ArgumentOutOfRangeException>(() => UtmProjection.FromLla(new Lla(84.1, 10, 0)));
}

public class UpsProjectionTests
{
    [Fact]
    public void Poles_sit_on_the_false_origin()
    {
        UpsCoordinate north = UpsProjection.FromLla(new Lla(90, 25, 3));
        Assert.True(north.IsNorthernHemisphere);
        GeoAssert.Meters(2_000_000, north.Easting, 1e-4);
        GeoAssert.Meters(2_000_000, north.Northing, 1e-4);
        GeoAssert.Meters(3, north.HeightMeters, 1e-9);

        UpsCoordinate south = UpsProjection.FromLla(new Lla(-90, -40, -2));
        Assert.False(south.IsNorthernHemisphere);
        GeoAssert.Meters(2_000_000, south.Easting, 1e-4);
        GeoAssert.Meters(2_000_000, south.Northing, 1e-4);
    }

    [Theory]
    [InlineData(85, 10, true, 2096454.1638, 1452981.2545)]
    [InlineData(88, -90, true, 1777930.7311, 2000000.0000)]
    [InlineData(-85, 10, false, 2096454.1638, 2547018.7455)]
    [InlineData(-88, -90, false, 1777930.7311, 2000000.0000)]
    public void Forward_matches_published_grid(double lat, double lon, bool northern, double easting, double northing)
    {
        UpsCoordinate ups = UpsProjection.FromLla(new Lla(lat, lon, 1));
        Assert.Equal(northern, ups.IsNorthernHemisphere);
        GeoAssert.Meters(easting, ups.Easting, 1e-3);
        GeoAssert.Meters(northing, ups.Northing, 1e-3);

        Lla back = UpsProjection.ToLla(ups);
        GeoAssert.Degrees(lat, back.LatitudeDeg, 1e-7);
        GeoAssert.Degrees(lon, back.LongitudeDeg, 1e-7);
        GeoAssert.Meters(1, back.HeightMeters, 1e-9);
    }

    [Fact]
    public void Mid_latitude_is_rejected() =>
        Assert.Throws<ArgumentOutOfRangeException>(() => UpsProjection.FromLla(new Lla(45, 10, 0)));
}
