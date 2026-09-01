using Minecraft.Core.Worlds.Biomes;

namespace Minecraft.Core.Worlds.Structures;

public readonly record struct TerrainColumn(int SurfaceY, Biome Biome);
