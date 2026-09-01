using Minecraft.Core.Worlds.Blocks;
using Minecraft.Core.Worlds.Decoration;
using Minecraft.Core.Worlds.Structures;

namespace Minecraft.Core.Worlds.Biomes;

public abstract class Biome
{
    public Block TopBlock { get; protected set; } = BlockRegistry.Stone;

    public Block GradientBlock { get; protected set; } = BlockRegistry.Stone;

    public Block CliffBlock { get; protected set; } = BlockRegistry.Stone;

    public int BaseHeight { get; protected set; }

    public bool HasShoreline { get; protected set; } = true;

    public IDecorator Decorator { get; protected set; } = new EmptyDecorator();

    public StructurePalette? SettlementPalette { get; protected set; }

    public double Temperature { get; protected set; }

    public double Moisture { get; protected set; }

    protected Biome()
    {
        DefineProperties();
    }

    public abstract double OffsetAt(int worldX, int worldZ);

    public virtual (Block Top, Block Gradient) SurfaceAt(int surfaceY) => (TopBlock, GradientBlock);

    public virtual Block CliffAt(int surfaceY) => CliffBlock;

    protected abstract void DefineProperties();
}
