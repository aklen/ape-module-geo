# Geo

**Ape.Module.Geo** is an optional Ape module: it converts a point between an external coordinate frame and an internal local ENU frame (east, north, up, meters). WGS-84 uses a spherical Earth and a configured origin. Local Cartesian passes values through. Unity maps a left-handed Y-up frame onto that ENU frame.

It is not part of Core. Core stays the scene, replica, and plugin host. This module is a library you add when a process needs those frames. It does not register a host service or a scene entity.

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

## Transforms

Every transform implements `ICoordinateTransform`: `ToInternal`, `FromInternal`, and `CoordinateSystemName`.

| Type | Name | Mapping |
| --- | --- | --- |
| `Wgs84Transform` | `WGS-84` | Latitude, longitude, altitude (degrees, degrees, meters) to local ENU meters from the origin, and back |
| `LocalCartesianTransform` | `Local-Cartesian` | Passthrough. X, Y, Z stay in meters |
| `UnityTransform` | `Unity3D` | Unity `(x, y, z)` to ENU `(x, z, y)`, and back |

`Wgs84Transform` takes an origin as latitude, longitude, and altitude. Each defaults to `0`. East and north use a sphere of radius 6371000 m. Altitude is the up axis, relative to the origin altitude. This is a local spherical approximation, not an ellipsoidal WGS-84 datum.

The origin and which transform to construct belong to the caller. The module does not hard-code a site.

## License

Copyright (c) 2026 [Akos Hamori](https://github.com/aklen).

Licensed under the [Mozilla Public License 2.0 (MPL-2.0)](https://www.mozilla.org/MPL/2.0/).
