namespace Ape.Module.Geo.Tests;

internal static class GeoAssert
{
    public static void Meters(double expected, double actual, double tolerance = 1e-3) =>
        Assert.InRange(actual, expected - tolerance, expected + tolerance);

    public static void Degrees(double expected, double actual, double tolerance = 1e-8) =>
        Assert.InRange(actual, expected - tolerance, expected + tolerance);
}
