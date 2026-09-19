using Minecraft.Core.Utilities.Vectors;
using Minecraft.Core.Worlds.Blocks.States;
using OpenTK.Mathematics;

namespace Minecraft.Core.Worlds.Blocks.Types;

public sealed class BlockLava : BlockFluid
{
    public const int MaxFlowLevel = 3;

    public const int ContactDamage = 4;

    public BlockLava(ushort id, int level, bool falling) : base(id, level, MaxFlowLevel, falling)
    {
    }

    protected override int FlowDelayTicks => 30;

    protected override bool FormsSourcesBetweenSprings => false;

    protected override Block FallingBlock => BlockRegistry.LavaFalling;

    public override Block GetForLevel(int level)
    {
        return level switch
        {
            0 => BlockRegistry.Lava,
            1 => BlockRegistry.LavaFlowing1,
            2 => BlockRegistry.LavaFlowing2,
            3 => BlockRegistry.LavaFlowing3,
            _ => BlockRegistry.Air,
        };
    }

    public override BlockState GetNewDefaultState()
    {
        return new BlockStateLava(this);
    }

    protected override bool TryReactWithNeighbours(World world, Vector3i blockPos)
    {
        if (!TouchesWater(world, blockPos))
        {
            return false;
        }

        Block hardened = IsSource ? BlockRegistry.Obsidian : BlockRegistry.Cobblestone;
        world.QueueToAddBlockAt(blockPos, BlockRegistry.GetState(hardened));
        return true;
    }

    protected override void FlowIntoOtherFluid(World world, Vector3i blockPos)
    {
        world.QueueToAddBlockAt(blockPos, BlockRegistry.GetState(BlockRegistry.Stone));
    }

    private static bool TouchesWater(World world, Vector3i blockPos)
    {
        if (world.GetBlockAt(blockPos.Up()).GetBlock() is BlockWater)
        {
            return true;
        }

        foreach (Vector3i sideOffset in _sideOffsets)
        {
            if (world.GetBlockAt(blockPos + sideOffset).GetBlock() is BlockWater)
            {
                return true;
            }
        }

        return false;
    }
}
