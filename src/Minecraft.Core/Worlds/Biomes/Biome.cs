using Minecraft.Core.Worlds.Blocks;
using Minecraft.Core.Worlds.Decoration;
using Minecraft.Core.Worlds.Structures;

namespace Minecraft.Core.Worlds.Biomes;

public abstract class Biome
{
    /// <summary>The block that forms the surface layer.</summary>
    public Block TopBlock { get; protected set; } = BlockRegistry.Stone;

    /// <summary>The block filling the few layers between the surface and the stone below it.</summary>
    public Block GradientBlock { get; protected set; } = BlockRegistry.Stone;

    /// <summary>
    /// What is left bare where the ground falls away too steeply for anything to settle on it. Soil slides off
    /// a cliff face, so what shows there is the rock the biome is cut into rather than its surface block.
    /// </summary>
    public Block CliffBlock { get; protected set; } = BlockRegistry.Stone;

    /// <summary>Height offset from sea level before terrain noise is applied.</summary>
    public int BaseHeight { get; protected set; }

    public IDecorator Decorator { get; protected set; } = new EmptyDecorator();

    /// <summary>
    /// The blocks villages in this biome are built from. Null where the terrain is no place to settle, which
    /// keeps villages out of that biome entirely.
    /// </summary>
    public StructurePalette? SettlementPalette { get; protected set; }

    /// <summary>Where this biome sits in climate space, in the same 0..1 range as the climate noise.</summary>
    public double Temperature { get; protected set; }

    /// <summary>Where this biome sits in climate space, in the same 0..1 range as the climate noise.</summary>
    public double Moisture { get; protected set; }

    protected Biome()
    {
        DefineProperties();
    }

    /// <summary>
    /// The terrain height offset this biome contributes at a world column, measured from sea level.
    /// </summary>
    public abstract double OffsetAt(int worldX, int worldZ);

    protected abstract void DefineProperties();
}
