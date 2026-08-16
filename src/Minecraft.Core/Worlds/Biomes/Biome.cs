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

    /// <summary>
    /// Whether ground this biome holds at the waterline is washed down to bare sand, the way a shore is.
    /// True nearly everywhere, and false for the biomes that are meant to be wet: a marsh that beached itself
    /// wherever it met its own water would be a field of sand with puddles in it rather than a marsh.
    /// </summary>
    public bool HasShoreline { get; protected set; } = true;

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

    /// <summary>
    /// What a column of this biome wears on top and immediately underneath, at the height its surface came
    /// out at. Height is passed in for the biomes whose ground is laid down in bands, where what shows
    /// depends on how high up the column stands rather than on the biome alone.
    /// </summary>
    public virtual (Block Top, Block Gradient) SurfaceAt(int surfaceY) => (TopBlock, GradientBlock);

    /// <summary>
    /// What is left bare on a face too steep to hold anything, at the height that face stands at. Banded the
    /// same way <see cref="SurfaceAt"/> is, since a cliff is where those bands are actually on show.
    /// </summary>
    public virtual Block CliffAt(int surfaceY) => CliffBlock;

    protected abstract void DefineProperties();
}
