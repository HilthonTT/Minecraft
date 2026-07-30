# Minecraft

A voxel game engine written in C# with OpenGL and GLSL, built on [OpenTK](https://opentk.net/).

This is an early stage project. The engine foundations — logging, binary IO, model loading, texture and VAO
handling, and noise generation for terrain — are in place; the world model and renderer are not yet written.

## Requirements

- [.NET SDK 10](https://dotnet.microsoft.com/download) or newer (developed against 10.0.302)
- A GPU and driver supporting OpenGL 3.0 or newer (vertex array objects)

The solution uses the `.slnx` format, which needs a recent SDK. Building the projects directly works with any
.NET 10 SDK.

## Building

```sh
dotnet build
dotnet run --project src/Minecraft.App
```

See [Status](#status) for the current build break.

## Layout

| Project          | Purpose                                                     |
| ---------------- | ----------------------------------------------------------- |
| `Minecraft.Core` | Engine library: everything below                             |
| `Minecraft.App`  | Executable entry point                                       |

Inside `Minecraft.Core`:

| Directory           | Contents                                                                   |
| ------------------- | -------------------------------------------------------------------------- |
| `IO/`               | Buffered binary writer for save files and network packets                   |
| `Logging/`          | Levelled console logger                                                     |
| `Render/`           | Vertex buffer layouts for chunk meshes                                      |
| `Resources/`        | Textures, fonts and models, copied to the output directory on build         |
| `Utilities/`        | Math helpers, input polling, texture loading, VAO wrapper                   |
| `Utilities/Models/` | Wavefront `.obj` loader                                                     |
| `Utilities/Noise/`  | Perlin noise used for terrain generation                                    |
| `World/Lighting/`   | Block lighting                                                              |

## Noise

Three generators live in `Minecraft.Core.Utilities.Noise`, all returning values in `[-1, 1]`. The two fractal
generators also expose `Noise01` for the `[0, 1]` range that height maps usually want. The single octave fields
are zero on the integer lattice, so scale block coordinates down before sampling rather than feeding them in
whole.

| Type                   | Use                                                            |
| ---------------------- | -------------------------------------------------------------- |
| `Noise2DPerlin`        | Single octave 2D gradient noise                                 |
| `Noise2DPerlinOctave`  | Fractal brownian motion over the above, for terrain height maps |
| `Noise3DPerlin`        | Improved 3D Perlin noise, for caves and ore distribution        |

```csharp
using Minecraft.Core.Utilities.Noise;

// Reseed once at world creation so a seed always reproduces the same world.
Noise2DPerlin.Reseed(worldSeed);
Noise3DPerlin.Reseed(worldSeed);

// Terrain height: 4 octaves of 2D noise, remapped to a block height.
float height = Noise2DPerlinOctave.Noise01(blockX * 0.01f, blockZ * 0.01f, octaves: 4);
int surfaceY = (int)(height * 64) + 32;

// Caves: carve where the 3D field crosses a threshold.
bool isCave = Noise3DPerlin.Noise(blockX * 0.05f, blockY * 0.05f, blockZ * 0.05f) > 0.6f;
```

Both octave generators take `octaves`, `persistence` (amplitude falloff per octave), `lacunarity` (frequency
gain per octave) and a starting `frequency`. Lower the frequency for larger features.

## Status

`Minecraft.Core` does not currently compile: `BufferedDataStream.WriteChunk` is written against `Chunk`,
`Section`, `BlockState` and `Constants`, and `WriteUtf8String` against `DataConverter.StringUtf8ToBytes`, none
of which exist yet. `Minecraft.App` is still a stub and does not reference `Minecraft.Core`.

## Credits

Based on [Ladadoos/3D-Voxel-Game-Engine](https://github.com/Ladadoos/3D-Voxel-Game-Engine/tree/master).

## License

MIT — see [LICENSE](LICENSE).
