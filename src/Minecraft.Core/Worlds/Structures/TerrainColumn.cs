using Minecraft.Core.Worlds.Biomes;

namespace Minecraft.Core.Worlds.Structures;

/// <summary>
/// What the generator produces at one world column, before caves and decoration.
/// </summary>
/// <param name="SurfaceY">The height of the topmost terrain block.</param>
/// <param name="Biome">The biome that supplies the surface blocks of the column.</param>
public readonly record struct TerrainColumn(int SurfaceY, Biome Biome);
