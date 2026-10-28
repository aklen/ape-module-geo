using Ape.Module.Geo.Geodesy;
using Ape.Module.Geo.Transforms;

namespace Ape.Module.Geo.Tests;

public class VincentyTests
{
    // Flinders Peak to Buninyong, the pair from Vincenty's 1975 paper.
    private static readonly Lla Flinders = new(-(37 + 57.0 / 60 + 3.72030 / 3600), 144 + 25.0 / 60 + 29.52440 / 3600, 0);
    private static readonly Lla Buninyong = new(-(37 + 39.0 / 60 + 10.15610 / 3600), 143 + 55.0 / 60 + 35.38390 / 3600, 0);

    [Fact]
    public void Inverse_matches_the_published_flinders_line()
    {
        GeodesicInverse inverse = Vincenty.Inverse(Flinders, Buninyong);
        Assert.True(inverse.Converged);
        GeoAssert.Meters(54972.271, inverse.DistanceMeters, 0.001);
        GeoAssert.Degrees(306 + 52.0 / 60 + 5.37 / 3600, inverse.ForwardAzimuthDeg, 1e-4);
        GeoAssert.Degrees(127 + 10.0 / 60 + 25.07 / 3600, inverse.ReverseAzimuthDeg, 1e-3);
    }

    [Fact]
    public void Direct_lands_on_the_published_endpoint()
    {
        GeodesicDirect direct = Vincenty.Direct(Flinders, 306 + 52.0 / 60 + 5.37 / 3600, 54972.271);
        Assert.True(direct.Converged);
        GeoAssert.Degrees(Buninyong.LatitudeDeg, direct.LatitudeDeg, 1e-7);
        GeoAssert.Degrees(Buninyong.LongitudeDeg, direct.LongitudeDeg, 1e-7);
    }

    [Fact]
    public void Equator_arc_is_the_semi_major_axis()
    {
        GeodesicInverse inverse = Vincenty.Inverse(new Lla(0, 0, 0), new Lla(0, 1, 0));
        Assert.True(inverse.Converged);
        GeoAssert.Meters(Wgs84Ellipsoid.SemiMajorAxisMeters * Math.PI / 180.0, inverse.DistanceMeters, 1e-3);
        GeoAssert.Degrees(90, inverse.ForwardAzimuthDeg, 1e-6);
    }

    [Fact]
    public void Coincident_points_have_no_distance()
    {
        GeodesicInverse inverse = Vincenty.Inverse(new Lla(12, 40, 100), new Lla(12, 40, 5));
        Assert.True(inverse.Converged);
        GeoAssert.Meters(0, inverse.DistanceMeters, 1e-9);
    }

    [Fact]
    public void Near_antipodes_report_when_the_series_does_not_converge()
    {
        GeodesicInverse inverse = Vincenty.Inverse(new Lla(0, 0, 0), new Lla(0.5, 179.7, 0));
        Assert.False(inverse.Converged);
        Assert.True(double.IsNaN(inverse.DistanceMeters));
    }
}
