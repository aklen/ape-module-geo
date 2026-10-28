# Geo

**Ape.Module.Geo** is an optional Ape module: it converts positions between geodetic, Earth-centered, local, and map frames. The shared local frame is ENU (east, north, up, meters).

It is not part of Core. Core stays the scene, replica, and plugin host. This module is a library you add when a process needs those frames. It does not register a host service or a scene entity.

Latitude and longitude are degrees. Height is meters above the WGS-84 ellipsoid, not mean sea level. Geodetic and projected conversions use double precision. The `ICoordinateTransform` adapters round a local result to `Vector3`.

## Place in the ecosystem

| Piece | Role |
| --- | --- |
| [ape-skeleton](https://github.com/aklen/ape-skeleton) | Workspace vessel (`./ape sync`, `./ape build`) |
| [ape-core](https://github.com/aklen/ape-core) | Framework: scene, DI, plugins, commits |
| [ape-launcher](https://github.com/aklen/ape-launcher) | Host executable |
| **this repo** | Coordinate transforms |

After `./ape sync` the tree looks like:

```
ape-skeleton/
└── src/
    ├── Ape.Core/
    ├── Ape.Launcher/
    └── Ape.Modules/
        └── Ape.Module.Geo/   ← this repository
```

Enable it in `workspace.yaml` under `modules:` (`name: Ape.Module.Geo`, this GitHub URL). Other projects reference `Ape.Module.Geo.csproj`. Callers construct a transform and convert points. There is no host JSON section and no sample scene.

`GeoModuleIds.ModuleId` and `GeoModule.Id` are `Ape.Module.Geo`.

## Local frames

`ICoordinateTransform` maps an external `Vector3` onto local ENU and back. X, Y, Z on the way in are latitude degrees, longitude degrees, and height meters, except for the two Cartesian adapters below.

| Type | Name | Mapping |
| --- | --- | --- |
| `Wgs84Transform` | `WGS-84` | Spherical LLA to local ENU. Radius 6371000 m |
| `EllipsoidEnuTransform` | `WGS-84-Ellipsoid` | Same slots, on the WGS-84 ellipsoid. Double math, `Vector3` at the boundary |
| `LocalCartesianTransform` | `Local-Cartesian` | Passthrough. X, Y, Z stay in meters |
| `UnityTransform` | `Unity3D` | Unity `(x, y, z)` to ENU `(x, z, y)`, and back |

`Wgs84Transform` is the fast local approximation. East and north use a sphere, and height is the up axis relative to the origin. It is not an ellipsoidal datum conversion. Use `EllipsoidEnuTransform` or `TangentFrame` when the ellipsoid matters. For millimeters at the local frame, call `TangentFrame` directly and keep the `Enu` value: `Vector3` is single precision.

`TangentFrame` also converts ECEF to NED (north, east, down) at the same origin.

## Geodetic coordinates

`Wgs84Ellipsoid` holds the WGS-84 ellipsoid: semi-major axis 6378137 m, inverse flattening 298.257223563.

| Call | Result |
| --- | --- |
| `Wgs84Ellipsoid.ToEcef` / `ToLla` | Geodetic LLA and Earth-centered Cartesian meters |
| `TangentFrame.LlaToEnu` / `EnuToLla` | LLA and local ENU at an origin |
| `TangentFrame.LlaToNed` / `NedToLla` | LLA and local NED at an origin |

`Lla`, `Ecef`, `Enu`, and `Ned` are the value types.

## Map projections

Projected easting and northing are meters. Height is copied and is not part of the projection.

| Type | Frame |
| --- | --- |
| `WebMercatorProjection` | EPSG:3857. Sphere radius 6378137 m. Latitude limit about ±85.051129° |
| `UtmProjection` | WGS-84 UTM from 80°S through 84°N. Scale 0.9996, false easting 500000 m, southern false northing 10000000 m |
| `UpsProjection` | WGS-84 UPS. North of 84°N and south of 80°S. False origin 2000000 m, scale 0.994 |

`UtmProjection.ZoneFor` picks the zone, including the widened zones along Norway and Svalbard. `FromLla(lla, zone)` forces a neighboring zone. Longitude 180° is zone 60 and longitude −180° is zone 1. `UtmCoordinate` carries the zone and the hemisphere. `UpsCoordinate` carries which polar zone.

## Geodesic

`Vincenty.Inverse` returns ellipsoidal distance and the forward and reverse azimuths between two LLA points. `Vincenty.Direct` walks a distance along an azimuth. Azimuth is degrees clockwise from north. Height is ignored.

Near the antipode the inverse series does not converge. `Converged` is false and the numeric fields are NaN.

## License

Copyright (c) 2026 [Akos Hamori](https://github.com/aklen).

Licensed under the [Mozilla Public License 2.0 (MPL-2.0)](https://www.mozilla.org/MPL/2.0/).
