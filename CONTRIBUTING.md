# Contributing

Thanks for taking an interest. This is a hobby voxel engine, so contributions of any size are welcome —
a fixed lighting seam is as useful as a new biome.

By taking part you agree to the [Code of Conduct](CODE_OF_CONDUCT.md).

## Getting set up

You need the [.NET SDK 10](https://dotnet.microsoft.com/download) or newer, Windows, and a GPU with OpenGL 3.3.
Both projects target `net10.0-windows` because texture loading goes through `System.Drawing`, so there is no
Linux or macOS build to fall back on.

```sh
git clone https://github.com/HilthonTT/Minecraft.git
cd Minecraft
dotnet build Minecraft.slnx
dotnet run --project src/Minecraft.App
```

The solution file uses the `.slnx` format, which needs a recent SDK. If your tooling does not understand it,
build the two `.csproj` files directly.

`README.md` covers the start arguments and the controls; `DESIGN.md` covers the layout and how the client and
server, world generation, lighting and the save format fit together — worth reading before a first change.

## Before you open a pull request

Small fixes can go straight to a pull request. For anything larger — a new mob, a change to the save format, a
different approach to meshing or lighting — open an issue first so the design can be talked through before you
spend an evening on it.

## Making a change

- **Build both configurations.** `dotnet build Minecraft.slnx --configuration Debug` and again with `Release`.
  CI does the same, along with a check that every asset the game loads by a literal path was copied next to the
  built assembly.
- **Run the tests.** `dotnet test Minecraft.slnx`, which CI runs in both configurations. They cover what can
  be checked without a window: stacks and slots, crafting, harvesting, the packet and save formats, and the
  start arguments. A change to any of those should arrive with the test that would have caught the old
  behaviour. Ids are the sharpest of them — `ItemRegistryTests` fails when a block or item id moves, because
  moving one changes what every existing save and every packet already means.
- **New assets need a csproj entry.** Shaders and resources are read at runtime by a path relative to the
  executable. A file that is not copied to the output directory compiles perfectly well and then fails on
  launch, usually as a `NullReferenceException` several layers from the real cause. Add it under the existing
  `None ... CopyToOutputDirectory="PreserveNewest"` items in `Minecraft.Core.csproj`.
- **Actually run the game.** A compiling change tells you very little about a renderer. Load a world, look at
  what you changed, and say in the pull request which seed you used.
- **Test multiplayer if you touched the server.** The world only ever changes on the server; clients request
  and wait for the broadcast. Singleplayer runs a server in the same process, so it exercises the same code
  path, but a change to `Network/` deserves a real second client. The `Dedicated server` and
  `Client (connect to localhost)` launch profiles are set up for exactly this.
- **Keep to the existing style.** File-scoped namespaces, Allman braces, four spaces, `var` only where the type
  is already on the right hand side of the assignment. `.editorconfig` describes the rest; `dotnet format` will
  apply it.
- **Comment the why, not the what.** The codebase explains reasoning that is not obvious from the code — why
  lighting repairs a region rather than a chunk, why a mesh is handed over one at a time. Follow that; skip
  comments that restate the line below them.
- **No new compiler warnings.** Nullable reference types are enabled in both projects, so please don't silence
  a warning with `!` where the nullability can be expressed properly instead.

## Performance

This is a game loop, and a lot of the code runs per frame, per chunk or per block. Allocating inside those
paths shows up as stutter rather than as a slow benchmark. There is an `ObjectPool` and reusable buffers for
this reason — prefer them to a fresh `List` per frame, and keep an eye on what LINQ allocates in hot code.

## Commits and pull requests

Commit messages follow [Conventional Commits](https://www.conventionalcommits.org/), matching the existing
history:

```
feat: New mobs (cow, pig) and improved mob spawning
fix: Lighting seam at chunk borders after a block break
perf: Reuse the vertex buffer when a chunk remeshes
docs: Document the noise generators
chore: Bump OpenTK to 4.9.5
```

Keep one pull request to one concern, fill in the template, and attach screenshots for anything visible. A
before-and-after pair is worth a lot for a rendering change.

## Reporting bugs

Use the issue templates. For a world generation or rendering bug, the seed and a screenshot are usually the
difference between a fix in an evening and one that never happens.

Security issues go through [SECURITY.md](SECURITY.md) rather than a public issue.

## Licence

Contributions are licensed under the [MIT Licence](LICENSE), the same as the rest of the project.
