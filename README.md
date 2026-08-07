# Minecraft

[![CI](https://github.com/HilthonTT/Minecraft/actions/workflows/ci.yml/badge.svg)](https://github.com/HilthonTT/Minecraft/actions/workflows/ci.yml)
[![CodeQL](https://github.com/HilthonTT/Minecraft/actions/workflows/codeql.yml/badge.svg)](https://github.com/HilthonTT/Minecraft/actions/workflows/codeql.yml)
[![.NET 10](https://img.shields.io/badge/.NET-10.0-512BD4)](https://dotnet.microsoft.com/download)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](LICENSE)

A voxel game engine written in C# with OpenGL and GLSL, built on [OpenTK](https://opentk.net/).

Infinite procedurally generated terrain across six biomes, with oceans and the rivers that run down to them,
ridged mountain ranges under snow and ice, buried ore, caves and villages. Coloured block lighting with smooth
per vertex ambient occlusion, distance fog, positional sound, a day/night cycle, mobs, and a client/server
architecture that the singleplayer mode also runs through.

![Sheep grazing on a terraced meadow scattered with roses and dandelions, oak trees to either side and a bare stone mountain rising behind it under a clear sky](Screenshots/sample-1.png)

**[DESIGN.md](DESIGN.md)** covers how it all works — terrain generation, lighting, meshing, networking, the
save format and the sound engine.

## Requirements

- [.NET SDK 10](https://dotnet.microsoft.com/download) or newer
- Windows — texture loading goes through `System.Drawing`, so both projects target `net10.0-windows`
- A GPU and driver supporting OpenGL 3.3 or newer

The solution uses the `.slnx` format, which needs a recent SDK. Building the projects directly works with any
.NET 10 SDK.

## Running

```sh
dotnet build
dotnet run --project src/Minecraft.App
```

That opens the main menu. `Singleplayer` lists your saved worlds, most recently played first — a row plays its
world and carries `Rename` and `Delete`, and `Create New World` names a world and picks its seed. `Escape`
during a game opens the pause menu, which is where you save and leave a world or quit.

In Visual Studio, pick a profile from the launch dropdown — `Singleplayer`, `Dedicated server` and
`Client (connect to localhost)` are defined in `src/Minecraft.App/Properties/launchSettings.json` and go
straight into a game without the menu.

### Worlds and seeds

The name offered on the create screen is one nothing is saved under yet, so pressing play generates a world;
type the name of a world you already have and it is carried on instead. The screen says which of the two it
would do, because a seed only ever decides a world that does not exist yet — an existing one keeps the seed it
was made with.

The seed box takes a number, or any words, which are hashed into one. Leave it empty for a random seed;
`Random` fills one in so you can read it off first. Either way the seed is repeated in the chat on the way in,
so a world worth revisiting can be written down.

### Playing together

A hosted world listens on every network interface, so the singleplayer world and the one friends join are the
same thing. Start a game, and the chat says which address to give them; they pick `Multiplayer`, type it in and
connect. `Host Game` on the multiplayer screen does exactly what `Singleplayer` does, and is there so that
hosting is findable from the screen where it is wanted. For a world with nobody playing on the machine that
runs it, use a dedicated server instead.

### Start arguments

Every argument is optional; anything left out uses its default.

| Argument   | Values                             | Default        | Purpose                                 |
| ---------- | ---------------------------------- | -------------- | --------------------------------------- |
| `mode`     | `client`, `server`, `clientserver` | `clientserver` | `clientserver` is singleplayer          |
| `ip`       | Host to connect to                 | `127.0.0.1`    | A server always listens on every one    |
| `port`     | `1`–`65535`                        | `25565`        |                                         |
| `world`    | Save directory name                | `world`        | Which world to load or create           |
| `seed`     | Any whole number                   | random         | Only used when creating a new world     |
| `menu`     | `true`, `false`                    | `true`         | `false` skips the main menu             |
| `fresh`    | `true`, `false`                    | `false`        | Deletes `world` first, then regenerates |
| `loglevel` | `packet`, `info`, `warn`, `error`  | `error`        | `packet` traces network traffic         |

```sh
dotnet run --project src/Minecraft.App -- mode=server port=25565 loglevel=info
dotnet run --project src/Minecraft.App -- world=canyons seed=12345
```

`server` runs headless. `client` connects to a server started separately. `world`, `seed` and `fresh` describe
the world a game started from the arguments opens; a game started from the menu takes its name and seed from
the menu instead, and never deletes anything.

## Controls

| Input           | Action                                     |
| --------------- | ------------------------------------------ |
| `W` `A` `S` `D` | Move                                       |
| Mouse           | Look                                       |
| `Space`         | Jump, swim upwards, or ascend while flying |
| `Space` twice   | Toggle flying                              |
| `Shift`         | Crouch, or descend while flying            |
| `Ctrl`          | Sprint                                     |
| Left click      | Break block                                |
| Right click     | Place block, or interact (TNT)             |
| Middle click    | Pick the block being looked at             |
| `Enter`         | Open and send chat                         |
| `Escape`        | Open the pause menu, or close the chat     |

Function keys drive the debug tools: `F1` hitboxes, `F2` debug readout, `F3` collect garbage, `F4` clear blocks
around the player, `F5` chunk borders, `F6` detached overhead camera, `F7` light level overlay (`Up`/`Down`
picks the level), `F8` fill a chunk layer with TNT, `F9` build a test room.

## Saved worlds

Worlds live in `saves/<name>/` next to the executable, and are written by whichever side runs the server, so
singleplayer and a dedicated server save identically. A world is saved when a chunk unloads, every 60 seconds,
and on a clean exit — leave through the pause menu or by closing the window rather than killing the process, or
anything since the last autosave is lost.

Only chunks that were actually modified are stored; everything else is regenerated from the seed on demand. See
[DESIGN.md](DESIGN.md#saved-worlds) for the format and what that costs.

## Layout

| Project          | Purpose                |
| ---------------- | ---------------------- |
| `Minecraft.Core` | Engine library         |
| `Minecraft.App`  | Executable entry point |

The directories inside `Minecraft.Core` are listed in [DESIGN.md](DESIGN.md#project-layout).

## Contributing

See [CONTRIBUTING.md](CONTRIBUTING.md), and read [DESIGN.md](DESIGN.md) first if you are touching world
generation — determinism there is load bearing, and the rules are written down.

## Credits

Based on [Ladadoos/3D-Voxel-Game-Engine](https://github.com/Ladadoos/3D-Voxel-Game-Engine/tree/master) and
[Minecraft](https://www.minecraft.net/en-us).

## License

MIT — see [LICENSE](LICENSE).
