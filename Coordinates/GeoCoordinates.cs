namespace Ape.Module.Geo;

/// <summary>Geodetic latitude and longitude in degrees, and ellipsoidal height in meters.</summary>
public readonly record struct Lla(double LatitudeDeg, double LongitudeDeg, double HeightMeters);

/// <summary>Earth-centered, Earth-fixed Cartesian position in meters.</summary>
public readonly record struct Ecef(double X, double Y, double Z);

/// <summary>Local east, north, up offset in meters.</summary>
public readonly record struct Enu(double East, double North, double Up);

/// <summary>Local north, east, down offset in meters.</summary>
public readonly record struct Ned(double North, double East, double Down);

/// <summary>Projected easting and northing in meters, with ellipsoidal height passed through.</summary>
public readonly record struct ProjectedPoint(double Easting, double Northing, double HeightMeters);
