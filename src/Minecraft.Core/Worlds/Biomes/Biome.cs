using Minecraft.Core.Worlds.Blocks;
using Minecraft.Core.Worlds.Decoration;

namespace Minecraft.Core.Worlds.Biomes;

public abstract class Biome
{
    /// <summary>The block that forms the surface layer.</summary>
    public Block TopBlock { get; protected set; } = BlockRegistry.Stone;

    /// <summary>The block filling the few layers between the surface and the stone below it.</summary>
    public Block GradientBlock { get; protected set; } = BlockRegistry.Stone;

    /// <summary>Height offset from sea level before terrain noise is applied.</summary>
    public int BaseHeight { get; protected set; }

    public IDecorator Decorator { get; protected set; } = new EmptyDecorator();

    /// <summary>Where this biome sits in climate space, in the same 0..1 range as the climate noise.</summary>
    public double Temperature { get; protected set; }

    /// <summary>Where this biome sits in climate space, in the same 0..1 range as the climate noise.</summary>
    public double Moisture { get; protected set; }

    protected Biome()
    {
        DefineProperties();
    }

    /// <summary>
    /// The terrain height offset this biome contributes at a chunk local column.
    /// </summary>
    public abstract double OffsetAt(int chunkX, int chunkZ, int localX, int localZ);

    protected abstract void DefineProperties();
}
