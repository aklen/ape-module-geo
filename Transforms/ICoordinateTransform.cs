using System.Numerics;

namespace Ape.Module.Geo.Transforms;

/// <summary>
/// Bidirectional mapping between an external coordinate system and an internal <see cref="Vector3"/> convention.
/// </summary>
/// <remarks>
/// Used for WGS-84 derivations, local Cartesian sites, or engine-specific frames.
/// </remarks>
public interface ICoordinateTransform
{
    /// <summary>Converts a point from the external system into internal coordinates.</summary>
    Vector3 ToInternal(Vector3 sourceCoords);

    /// <summary>Converts a point from internal coordinates back to the external system.</summary>
    Vector3 FromInternal(Vector3 internalCoords);

    /// <summary>Gets a short, human-readable name of the source coordinate system for diagnostics and UI.</summary>
    string CoordinateSystemName { get; }
}
