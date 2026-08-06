using Minecraft.Core.Audio;
using Minecraft.Core.Physics;
using Minecraft.Core.Utilities.Vectors;
using Minecraft.Core.Worlds.Blocks.States;
using OpenTK.Mathematics;

namespace Minecraft.Core.Worlds.Blocks.Types;

public sealed class BlockWheat : Block
{
    private const ushort MaxMaturity = 2;
    private const float SecondsToGrow = 3.0F;

    public BlockWheat(ushort id) : base(id)
    {
        IsTickable = true;
        IsOpaque = false;
        HasCustomState = true;
        SoundMaterial = BlockSoundMaterial.Grass;
    }

    public override BlockState GetNewDefaultState()
    {
        return new BlockStateWheat();
    }

    public override void OnTick(BlockState blockState, World world, Vector3i blockPos, float deltaTime)
    {
        if (world is not WorldServer)
        {
            return;
        }

        var wheat = (BlockStateWheat)blockState;
        if (wheat.Maturity >= MaxMaturity)
        {
            return;
        }

        wheat.ElapsedTimeSinceLastGrowth += deltaTime;
        if (wheat.ElapsedTimeSinceLastGrowth < SecondsToGrow)
        {
            return;
        }

        wheat.ElapsedTimeSinceLastGrowth = 0;

        // Maturity is baked into the mesh, so growing means replacing the block rather than mutating it.
        world.QueueToRemoveBlockAt(blockPos);
        var grownWheat = (BlockStateWheat)BlockRegistry.GetState(BlockRegistry.Wheat);
        grownWheat.Maturity = (ushort)(wheat.Maturity + 1);
        world.QueueToAddBlockAt(blockPos, grownWheat);
    }

    public override AxisAlignedBox[] GetCollisionBox(BlockState state, Vector3i blockPos)
    {
        return _emptyAABB;
    }

    public override bool CanAddBlockAt(World world, Vector3i blockPos)
    {
        return world.GetBlockAt(blockPos.Down()).GetBlock() == BlockRegistry.Grass;
    }

    public override void OnNotify(
        BlockState blockState,
        BlockState sourceBlockState,
        World world,
        Vector3i blockPos,
        Vector3i sourceBlockPos)
    {
        if (world is not WorldServer)
        {
            return;
        }

        if (sourceBlockPos == blockPos.Down() && world.GetBlockAt(sourceBlockPos).GetBlock() == BlockRegistry.Air)
        {
            world.QueueToRemoveBlockAt(blockPos);
        }
    }
}
