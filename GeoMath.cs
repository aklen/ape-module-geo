namespace Ape.Module.Geo;

internal static class GeoMath
{
    public static double ToRadians(double degrees) => degrees * (Math.PI / 180.0);

    public static double ToDegrees(double radians) => radians * (180.0 / Math.PI);

    /// <summary>Wraps a longitude into [-180, 180]. Both antimeridian ends stay distinct.</summary>
    public static double NormalizeLongitude(double degrees)
    {
        if (!double.IsFinite(degrees))
            return degrees;

        double wrapped = Math.IEEERemainder(degrees, 360.0);
        if (wrapped == 0.0)
            return 0.0;

        return wrapped;
    }

    /// <summary>Wraps an azimuth into [0, 360).</summary>
    public static double NormalizeAzimuth(double degrees)
    {
        if (!double.IsFinite(degrees))
            return degrees;

        double wrapped = degrees % 360.0;
        if (wrapped < 0.0)
            wrapped += 360.0;

        return wrapped == 360.0 ? 0.0 : wrapped;
    }

    public static double WrapLongitudeDeltaRadians(double radians)
    {
        double wrapped = Math.IEEERemainder(radians, 2.0 * Math.PI);
        if (wrapped <= -Math.PI)
            wrapped += 2.0 * Math.PI;

        return wrapped;
    }

    public static double Atanh(double value) => 0.5 * Math.Log((1.0 + value) / (1.0 - value));

    public static void RequireFinite(double value, string paramName)
    {
        if (!double.IsFinite(value))
            throw new ArgumentOutOfRangeException(paramName, value, "Value must be finite.");
    }

    public static void RequireLatitude(double latitudeDeg, string paramName)
    {
        if (!double.IsFinite(latitudeDeg) || latitudeDeg < -90.0 || latitudeDeg > 90.0)
            throw new ArgumentOutOfRangeException(paramName, latitudeDeg, "Latitude must be a finite value from -90 to 90 degrees.");
    }

    public static void RequireLla(Lla lla, string paramName)
    {
        RequireLatitude(lla.LatitudeDeg, paramName);
        RequireFinite(lla.LongitudeDeg, paramName);
        RequireFinite(lla.HeightMeters, paramName);
    }
}
