# Design

How the engine works. For building and playing it, see [README.md](README.md).

- [Project layout](#project-layout)
- [Client and server](#client-and-server)
- [World generation](#world-generation)
- [Structures](#structures)
- [Saved worlds](#saved-worlds)
- [Lighting](#lighting)
- [Rendering](#rendering)
- [Mobs](#mobs)
- [Sound](#sound)
- [Noise](#noise)

## Project layout

| Project          | Purpose                |
| ---------------- | ---------------------- |
| `Minecraft.Core` | Engine library         |
| `Minecraft.App`  | Executable entry point |

Inside `Minecraft.Core`:

| Directory            | Contents                                                                 |
| -------------------- | ------------------------------------------------------------------------ |
| `Audio/`             | Sound loading, the mixer, and what decides when anything plays            |
| `Entities/`          | Camera, the player on both sides of a connection, and the mobs            |
| `Games/`             | Game loop, window, game state, menu flow, start argument parsing          |
| `IO/`                | Buffered binary reader and writer for network packets                     |
| `Logging/`           | Levelled console logger                                                   |
| `Network/`           | Client, server, sessions, packets and their handlers                      |
| `Physics/`           | Axis aligned boxes and ray tracing                                        |
| `Render/`            | Master renderer, chunk meshing, debug overlays, UI and the menu screens   |
| `Resources/`         | Textures, fonts and models, copied to the output directory on build       |
| `Shaders/`           | GLSL programs and their typed wrappers                                    |
| `Shapes/`            | Block and entity model geometry, and the registries that hold it          |
| `Textures/`          | Texture atlas and the offscreen framebuffer                               |
| `Utilities/`         | Math, input, noise, OBJ loading, VAO wrapper, object pool                 |
| `Worlds/`            | Blocks, chunks, sections and lighting                                     |
| `Worlds/Biomes/`     | The biomes and the climate map that decides between them                  |
| `Worlds/Generation/` | The generator itself: terrain sampling, ore veins and cave carving        |
| `Worlds/Decoration/` | What each biome grows on its surface, and the trees it grows              |
| `Worlds/Structures/` | Villages and the framework that sites them and builds them across chunks  |
| `Worlds/Storage/`    | Reading and writing saved worlds                                          |

## Client and server

The world only ever changes on the server. A client sends a request — place, break, interact — and applies
nothing until the server broadcasts the result back. Singleplayer runs the same path, with a server hosted in
the same process, so there is one code path rather than two.

Chunks are generated on a background thread and streamed per player, nearest first, by a `ChunkProvider` that
each session owns. A chunk stays loaded while at least one player can see it. Meshing runs on its own thread
and hands one finished mesh to the renderer at a time, which keeps the large vertex buffers reusable and stops
a chunk change from stalling a frame.

## World generation

Which biome a column belongs to is read off a climate map — temperature and moisture, each its own noise
field. The surface blocks come from whichever biome dominates, but the height is a blend of all of them, so a
border comes out as a slope rather than as a step.

| Biome       | Surface             | Character                                                     |
| ----------- | ------------------- | ------------------------------------------------------------- |
| Plains      | Grass over dirt     | Open and gentle, flowers through the grass, few trees          |
| Forest      | Grass over dirt     | Rolling, thick with oak and birch, mushrooms in the shade      |
| Savanna     | Grass over dirt     | Flat topped plateaus with a scramble between one and the next  |
| Desert      | Sand over sandstone | Dunes marching across a broad basin, cactus and dead bush      |
| Mountain    | Bare stone          | Ridged spines and gullies, patched with gravel and moss        |
| Snowy peaks | Snowy grass         | The highest ground there is, pine over its lower shoulders     |

### Land and sea

How much of a column is land at all is decided before any of that, by a much broader field than the climate:
where it falls away the ground is taken down into an ocean basin, and the biome's own hills are faded out along
with it, so a sea has a floor of its own rather than the hills of whatever biome nominally covers it carried on
under the waves. Between the two the ground climbs, which is what puts a shelf and then a beach between a sea
and the land behind it instead of a wall.

Rivers are cut along the line where a second, finer field crosses zero — a contour of a continuous field either
closes on itself or runs forever, so a river never stops halfway. A river only ever lowers ground and only cuts
so deep, so it carves a valley through the highlands and holds water once the land it crosses is low enough to
fill.

Everything left standing open below sea level is then filled with water, which is what puts the sea into a
basin, the water into a river and a lake into any hollow that happens to fall below it. Ground the water
reaches is washed to sand whichever biome it belongs to, and to gravel further down, so every shore has a beach
and a sea reads as getting deeper rather than as one flat basin. Nothing is grown on a seabed, and no village
is sited on one.

### Snow and cliffs

Above the snow line the ground goes under snow, with sheets of glacial ice across part of it. The line itself
wanders, so it does not draw a contour around every summit at exactly the same height. Anywhere the ground
drops three blocks or more from one column to the next is left as the bare rock of its biome, which is what
puts faces on the cliffs and keeps soil off them.

### Underground

Veins of coal, iron, gold, redstone and diamond are laid in bands by depth, along with pockets of dirt, gravel
and clay, and the occasional seam of glowstone that lights a cave when one breaks into it. A vein belongs to
the chunk its centre falls in but is laid down by every chunk it reaches into, so it comes out whole instead of
sheared off at a border. Caves are carved after the veins, so a tunnel cutting through one leaves its face
showing in the wall, and the floor of the world is bedrock.

![An oak village of plank and log houses beside a fenced wheat field, with sheep among the buildings, pine covered outcrops behind and grey mountains on the horizon](Screenshots/sample-2.png)

## Structures

A village is larger than a chunk, so every chunk it covers works out the whole layout from scratch and keeps
only its own slice; two chunks that disagreed would leave a house cut in half along the border.

An `IStructure` therefore has to be a pure function of the world seed and its position, and it reads the ground
through `ITerrainSampler` — which recomputes terrain from the seed — rather than out of the world, since the
neighbouring chunks it spans are not loaded while any one of them is being generated.

## Saved worlds

```
saves/world/
  level.dat          format version, seed and time of day, as plain text
  chunks/c.0.-1.gz   one gzipped chunk, named after its grid position
```

Only chunks that were actually **modified** are stored. Everything else is regenerated from the seed on demand,
which is both faster and far smaller than writing terrain nobody has touched — a world that has only been
walked across saves nothing at all.

This works because generation is fully deterministic: the noise fields are seeded from `seed`, and each chunk's
decoration draws from a `Random` seeded by mixing the world seed with the chunk position. Two worlds created
with the same seed generate byte-identical terrain.

> That determinism is load bearing. If you add generation code, drive every random choice from the `Random`
> handed to `IDecorator.Decorate`, never from an unseeded one, or stored chunks will stop matching their
> regenerated neighbours.

Renaming a world moves its directory and deleting one removes it outright, both only ever from the main menu,
where nothing is loaded to be moved out from under.

The save format is at version 2. Version 1 worlds are refused rather than opened: only modified chunks are
stored and the rest are regenerated, so a world made before the terrain was reshaped to hold water would come
back as half its old ground and half new ground that no longer joins onto it. A save whose `version` does not
match the running build is refused rather than misread, and a corrupt chunk file is reported and regenerated
instead of taking the world down with it.

Passing `seed` for a world that already exists is ignored, with a warning — its terrain is already fixed. To
start over, delete the world directory or pick a different `world` name.

## Lighting

Lighting is a flood fill over four channels — red, green, blue and sunlight — packed into a `ushort` per block.
Placing or breaking a block repairs only the affected region rather than relighting the chunk, and
`SmoothLighting` averages the neighbouring cells at mesh time to get per vertex gradients and ambient
occlusion.

## Rendering

### Water

A chunk is meshed into two buffers rather than one. The solid blocks go down first, and the water is drawn
after them and after the entities, blended over whatever ended up behind it, with depth writes off so one
stretch of water does not cut a hole in the stretch behind it and with culling off so its surface is still
there when looked at from underneath.

Two cells of the same liquid share no face, so the inside of a sea carries no geometry at all: what is drawn is
the skin where it meets the air. Under water the sky is left out entirely and the fog closes to a few blocks of
dim blue, which is what reads as having gone under.

### Fog

Distance is hazed over with fog, in the horizon colour of whatever hour the world is at, so that it turns
orange at sunset and near black at night along with the sky behind it. The distance it is measured over ignores
height, because that is the shape the world is loaded in — chunks are columns kept or dropped on how far away
they are horizontally.

It closes over completely at exactly one view distance, which is the nearest the edge of the loaded world can
ever be, so terrain dissolves into the sky instead of ending along a line. The loaded area is a square, so
measuring the fog as a radius also closes it over the corners first, and what is left visible reads as a circle
rather than as four straight edges.

## Mobs

Sheep graze by day and zombies come out after dark, both built as cuboid models wearing a Minecraft skin sheet.
Only the server runs their behaviour and physics; every client eases each mob towards the last position and
facing it was told about, the same way it does for other players.

Passive and hostile mobs are counted against caps of their own rather than one shared total. Sharing a cap
starves the animals out — a hostile mob follows the player and so never wanders far enough off to be despawned,
while an animal drifts away and is cleared. Hostile mobs left over from the night burn up a few seconds after
the sun finds them, wherever they are standing and whoever is watching.

Zombies only appear in the dark. The server keeps no light map — those are built by the renderer on each client
— so the spawner works out where the daylight falls from the heightmap every chunk already carries: the sun
comes down each column that has nothing solid over it and then spreads out from where it lands, losing a step
for every block it travels sideways and every block it drops. Anything more than seven steps from the open sky
is dark, which is what tells a cave from the shade of a tree. So by day they are confined to the caves, and at
night they have the run of the surface as well, apart from wherever a torch is burning.

![Zombies scattered across a dark meadow at night, one of them close to the camera](Screenshots/sample-3.png)

## Sound

Sounds are placed in the world: quieter the further off they are, weighted towards the ear they are on, and
dropped entirely past the distance anything can be heard from. Where the listener stands is the active camera
rather than the player, so the detached overhead camera hears the world from where it is actually looking at
it.

Almost all of it is decided on the client from what it can already see, so hardly any of it needed anything new
from the server. Footsteps come from watching things move rather than from being told about a step, which is
what lets another player's footfalls sound without a packet per stride. Every sound is drawn from a set of
interchangeable recordings and pitched slightly either side of where it was recorded, so a run across open
ground does not read as the same handful of clips looping.

| Sound             | When                                                  |
| ----------------- | ----------------------------------------------------- |
| Footsteps         | Anything walking, in the voice of the block underfoot  |
| Breaking, placing | A block changing, in that block's own material         |
| Splash, strokes   | Going into water, and swimming through it              |
| Animals, zombies  | Their own calls on a timer, and their own footfalls    |
| Fuse, explosion   | TNT being struck, and the blast at the end of it       |

A block says which of seven sets it belongs to — stone, grass, gravel, sand, wood, snow or cloth — rather than
carrying sounds of its own, since a dozen kinds of stone all break the same way. The greenery sounds like the
grass it grows out of; cloth is for the cactus, which gives under a blow rather than tearing.

The blast is the one thing the server has to send, as an `Explosion` packet carrying where it went off. What a
client would otherwise see of it is the hundreds of separate block removals it leaves behind, which arrive one
at a time and are indistinguishable from somebody mining quickly. Those removals are then held silent for a
moment, so what is heard is the bang rather than the terrain it destroyed being taken apart block by block.

The set on disk is far larger than what is used, so only what is reachable is read; that takes about two thirds
of a second, which is done off the main thread so the window is not held up by it. A machine with no sound
device logs it once and runs silently rather than refusing to start.

## Noise

Four generators live in `Minecraft.Core.Utilities.Noise`, all returning values in `[-1, 1]`, with `Noise01` for
the `[0, 1]` range that height maps usually want. The single octave fields are zero on the integer lattice, so
scale block coordinates down before sampling rather than feeding them in whole.

| Type                  | Use                                                             |
| --------------------- | --------------------------------------------------------------- |
| `Noise2DPerlin`       | Single octave 2D gradient noise                                  |
| `Noise2DPerlinOctave` | Fractal brownian motion over the above, for terrain height maps  |
| `Noise3DPerlin`       | Improved 3D Perlin noise, for caves and ore distribution         |
| `TerrainNoise`        | Ridges, terraces and distribution flattening built on the above  |

`TerrainNoise` is where the shapes that are not noise in themselves live. `Ridged01` folds a field about zero to
turn its smooth valleys into the creases a mountain range is built from, `Terrace01` cuts a range into plateaus
with an escarpment between them, and `Spread01` flattens the bell shaped distribution Perlin noise produces
into an even one — without which a climate map puts nearly the whole world into a single biome.

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
