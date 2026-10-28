using Ape.Module.Geo;
using Ape.Module.Geo.Transforms;

namespace Ape.Module.Geo.Geodesy;

/// <summary>Inverse geodesic on the WGS-84 ellipsoid. Azimuths are degrees clockwise from north.</summary>
/// <param name="Converged">False near the antipode, where this method does not converge. The other fields are then NaN.</param>
public readonly record struct GeodesicInverse(
    double DistanceMeters,
    double ForwardAzimuthDeg,
    double ReverseAzimuthDeg,
    bool Converged);

/// <summary>Direct geodesic on the WGS-84 ellipsoid.</summary>
/// <param name="Converged">False when the direct series does not converge. The other fields are then NaN.</param>
public readonly record struct GeodesicDirect(
    double LatitudeDeg,
    double LongitudeDeg,
    double ReverseAzimuthDeg,
    bool Converged);

/// <summary>
/// Vincenty direct and inverse geodesics on the WGS-84 ellipsoid.
/// Distances are ellipsoidal surface distances. Height is ignored.
/// </summary>
public static class Vincenty
{
    private const int MaxIterations = 100;
    private const double Tolerance = 1e-12;

    public static GeodesicInverse Inverse(Lla start, Lla end)
    {
        GeoMath.RequireLla(start, nameof(start));
        GeoMath.RequireLla(end, nameof(end));

        double a = Wgs84Ellipsoid.SemiMajorAxisMeters;
        double b = Wgs84Ellipsoid.SemiMinorAxisMeters;
        double f = Wgs84Ellipsoid.Flattening;
        double lat1 = GeoMath.ToRadians(start.LatitudeDeg);
        double lat2 = GeoMath.ToRadians(end.LatitudeDeg);
        double l = GeoMath.WrapLongitudeDeltaRadians(GeoMath.ToRadians(end.LongitudeDeg - start.LongitudeDeg));

        double u1 = Math.Atan((1.0 - f) * Math.Tan(lat1));
        double u2 = Math.Atan((1.0 - f) * Math.Tan(lat2));
        double sinU1 = Math.Sin(u1);
        double cosU1 = Math.Cos(u1);
        double sinU2 = Math.Sin(u2);
        double cosU2 = Math.Cos(u2);

        double lambda = l;
        double sinSigma = 0.0;
        double cosSigma = 0.0;
        double sigma = 0.0;
        double sinAlpha = 0.0;
        double cos2Alpha = 0.0;
        double cos2SigmaM = 0.0;
        bool converged = false;

        for (int i = 0; i < MaxIterations; i++)
        {
            double sinLambda = Math.Sin(lambda);
            double cosLambda = Math.Cos(lambda);
            double y = cosU1 * sinU2 - sinU1 * cosU2 * cosLambda;
            sinSigma = Math.Sqrt(cosU2 * sinLambda * (cosU2 * sinLambda) + y * y);
            cosSigma = sinU1 * sinU2 + cosU1 * cosU2 * cosLambda;
            if (sinSigma == 0.0)
            {
                if (cosSigma > 0.0)
                    return new GeodesicInverse(0.0, 0.0, 0.0, true);

                return NotConvergedInverse();
            }

            sigma = Math.Atan2(sinSigma, cosSigma);
            sinAlpha = cosU1 * cosU2 * sinLambda / sinSigma;
            cos2Alpha = 1.0 - sinAlpha * sinAlpha;
            cos2SigmaM = Math.Abs(cos2Alpha) < 1e-16 ? 0.0 : cosSigma - 2.0 * sinU1 * sinU2 / cos2Alpha;
            double c = f / 16.0 * cos2Alpha * (4.0 + f * (4.0 - 3.0 * cos2Alpha));
            double next = l + (1.0 - c) * f * sinAlpha
                * (sigma + c * sinSigma * (cos2SigmaM + c * cosSigma * (-1.0 + 2.0 * cos2SigmaM * cos2SigmaM)));
            if (Math.Abs(next - lambda) < Tolerance)
            {
                lambda = next;
                converged = true;
                break;
            }

            lambda = next;
        }

        if (!converged)
            return NotConvergedInverse();

        double sinLambdaFinal = Math.Sin(lambda);
        double cosLambdaFinal = Math.Cos(lambda);
        double yFinal = cosU1 * sinU2 - sinU1 * cosU2 * cosLambdaFinal;
        sinSigma = Math.Sqrt(cosU2 * sinLambdaFinal * (cosU2 * sinLambdaFinal) + yFinal * yFinal);
        cosSigma = sinU1 * sinU2 + cosU1 * cosU2 * cosLambdaFinal;
        sigma = Math.Atan2(sinSigma, cosSigma);
        sinAlpha = cosU1 * cosU2 * sinLambdaFinal / sinSigma;
        cos2Alpha = 1.0 - sinAlpha * sinAlpha;
        cos2SigmaM = Math.Abs(cos2Alpha) < 1e-16 ? 0.0 : cosSigma - 2.0 * sinU1 * sinU2 / cos2Alpha;

        double uSq = cos2Alpha * (a * a - b * b) / (b * b);
        double aa = 1.0 + uSq / 16384.0 * (4096.0 + uSq * (-768.0 + uSq * (320.0 - 175.0 * uSq)));
        double bb = uSq / 1024.0 * (256.0 + uSq * (-128.0 + uSq * (74.0 - 47.0 * uSq)));
        double deltaSigma = bb * sinSigma * (cos2SigmaM + bb / 4.0 * (
            cosSigma * (-1.0 + 2.0 * cos2SigmaM * cos2SigmaM)
            - bb / 6.0 * cos2SigmaM * (-3.0 + 4.0 * sinSigma * sinSigma) * (-3.0 + 4.0 * cos2SigmaM * cos2SigmaM)));
        double distance = b * aa * (sigma - deltaSigma);
        double forward = Math.Atan2(cosU2 * sinLambdaFinal, yFinal);
        double finalBearing = Math.Atan2(cosU1 * sinLambdaFinal, -sinU1 * cosU2 + cosU1 * sinU2 * cosLambdaFinal);

        return new GeodesicInverse(
            distance,
            GeoMath.NormalizeAzimuth(GeoMath.ToDegrees(forward)),
            GeoMath.NormalizeAzimuth(GeoMath.ToDegrees(finalBearing) + 180.0),
            true);
    }

    public static GeodesicDirect Direct(Lla start, double azimuthDeg, double distanceMeters)
    {
        GeoMath.RequireLla(start, nameof(start));
        GeoMath.RequireFinite(azimuthDeg, nameof(azimuthDeg));
        if (!double.IsFinite(distanceMeters) || distanceMeters < 0.0)
            throw new ArgumentOutOfRangeException(nameof(distanceMeters), distanceMeters, "Distance must be a finite value of zero or more meters.");

        if (distanceMeters == 0.0)
        {
            return new GeodesicDirect(
                start.LatitudeDeg,
                GeoMath.NormalizeLongitude(start.LongitudeDeg),
                GeoMath.NormalizeAzimuth(azimuthDeg + 180.0),
                true);
        }

        double a = Wgs84Ellipsoid.SemiMajorAxisMeters;
        double b = Wgs84Ellipsoid.SemiMinorAxisMeters;
        double f = Wgs84Ellipsoid.Flattening;
        double lat1 = GeoMath.ToRadians(start.LatitudeDeg);
        double lon1 = GeoMath.ToRadians(start.LongitudeDeg);
        double alpha1 = GeoMath.ToRadians(azimuthDeg);
        double sinAlpha1 = Math.Sin(alpha1);
        double cosAlpha1 = Math.Cos(alpha1);

        double tanU1 = (1.0 - f) * Math.Tan(lat1);
        double cosU1 = 1.0 / Math.Sqrt(1.0 + tanU1 * tanU1);
        double sinU1 = tanU1 * cosU1;
        double sigma1 = Math.Atan2(tanU1, cosAlpha1);
        double sinAlpha = cosU1 * sinAlpha1;
        double cosSqAlpha = 1.0 - sinAlpha * sinAlpha;
        double uSq = cosSqAlpha * (a * a - b * b) / (b * b);
        double aa = 1.0 + uSq / 16384.0 * (4096.0 + uSq * (-768.0 + uSq * (320.0 - 175.0 * uSq)));
        double bb = uSq / 1024.0 * (256.0 + uSq * (-128.0 + uSq * (74.0 - 47.0 * uSq)));

        double sigma = distanceMeters / (b * aa);
        double cos2SigmaM = 0.0;
        bool converged = false;
        for (int i = 0; i < MaxIterations; i++)
        {
            cos2SigmaM = Math.Cos(2.0 * sigma1 + sigma);
            double sinSigma = Math.Sin(sigma);
            double cosSigma = Math.Cos(sigma);
            double deltaSigma = bb * sinSigma * (cos2SigmaM + bb / 4.0 * (
                cosSigma * (-1.0 + 2.0 * cos2SigmaM * cos2SigmaM)
                - bb / 6.0 * cos2SigmaM * (-3.0 + 4.0 * sinSigma * sinSigma) * (-3.0 + 4.0 * cos2SigmaM * cos2SigmaM)));
            double next = distanceMeters / (b * aa) + deltaSigma;
            if (Math.Abs(next - sigma) < Tolerance)
            {
                sigma = next;
                cos2SigmaM = Math.Cos(2.0 * sigma1 + sigma);
                converged = true;
                break;
            }

            sigma = next;
        }

        if (!converged)
            return new GeodesicDirect(double.NaN, double.NaN, double.NaN, false);

        double sinSigmaFinal = Math.Sin(sigma);
        double cosSigmaFinal = Math.Cos(sigma);
        double tmp = sinU1 * sinSigmaFinal - cosU1 * cosSigmaFinal * cosAlpha1;
        double lat2 = Math.Atan2(
            sinU1 * cosSigmaFinal + cosU1 * sinSigmaFinal * cosAlpha1,
            (1.0 - f) * Math.Sqrt(sinAlpha * sinAlpha + tmp * tmp));
        double lambda = Math.Atan2(sinSigmaFinal * sinAlpha1, cosU1 * cosSigmaFinal - sinU1 * sinSigmaFinal * cosAlpha1);
        double c = f / 16.0 * cosSqAlpha * (4.0 + f * (4.0 - 3.0 * cosSqAlpha));
        double l = lambda - (1.0 - c) * f * sinAlpha
            * (sigma + c * sinSigmaFinal * (cos2SigmaM + c * cosSigmaFinal * (-1.0 + 2.0 * cos2SigmaM * cos2SigmaM)));
        double alpha2 = Math.Atan2(sinAlpha, -tmp);

        return new GeodesicDirect(
            GeoMath.ToDegrees(lat2),
            GeoMath.NormalizeLongitude(GeoMath.ToDegrees(lon1 + l)),
            GeoMath.NormalizeAzimuth(GeoMath.ToDegrees(alpha2) + 180.0),
            true);
    }

    private static GeodesicInverse NotConvergedInverse() =>
        new(double.NaN, double.NaN, double.NaN, false);
}
