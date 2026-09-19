namespace Minecraft.Core.Worlds.Blocks.Types;

public sealed class BlockWater : BlockFluid
{
    public const int MaxFlowLevel = 7;

    public BlockWater(ushort id, int level, bool falling) : base(id, level, MaxFlowLevel, falling)
    {
        IsTranslucent = true;
    }

    protected override int FlowDelayTicks => 5;

    protected override bool FormsSourcesBetweenSprings => true;

    protected override Block FallingBlock => BlockRegistry.WaterFalling;

    public override Block GetForLevel(int level)
    {
        return level switch
        {
            0 => BlockRegistry.Water,
            1 => BlockRegistry.WaterFlowing1,
            2 => BlockRegistry.WaterFlowing2,
            3 => BlockRegistry.WaterFlowing3,
            4 => BlockRegistry.WaterFlowing4,
            5 => BlockRegistry.WaterFlowing5,
            6 => BlockRegistry.WaterFlowing6,
            7 => BlockRegistry.WaterFlowing7,
            _ => BlockRegistry.Air,
        };
    }
}
