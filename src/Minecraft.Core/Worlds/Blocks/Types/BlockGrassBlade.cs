using Minecraft.Core.Audio;
using Minecraft.Core.Inventories;
using Minecraft.Core.Inventories.Items;
using Minecraft.Core.Physics;
using Minecraft.Core.Utilities.Vectors;
using Minecraft.Core.Worlds.Blocks.States;
using OpenTK.Mathematics;

namespace Minecraft.Core.Worlds.Blocks.Types;

public sealed class BlockGrassBlade : Block
{
    public BlockGrassBlade(ushort id) : base(id)
    {
        IsOpaque = false;
        IsOverridable = true;
        SoundMaterial = BlockSoundMaterial.Grass;
        SecondsToBreak = 0F;
    }

    public override BlockState GetNewDefaultState()
    {
        return new BlockStateGrassBlade();
    }

    public override AxisAlignedBox[] GetCollisionBox(BlockState state, Vector3i blockPos)
    {
        return _emptyAABB;
    }

    private const int SeedsOneIn = 8;

    // Wild grass is where the first seeds come from; there is nowhere else to find wheat before it is farmed.
    public override void AddDrops(BlockState blockState, World world, Vector3i blockPos, Random random, List<ItemStack> drops)
    {
        base.AddDrops(blockState, world, blockPos, random, drops);

        if (random.Next(SeedsOneIn) == 0)
        {
            drops.Add(new ItemStack(ItemRegistry.Seeds, 1));
        }
    }

    public override bool CanAddBlockAt(World world, Vector3i blockPos)
    {
        Block block = world.GetBlockAt(blockPos.Down()).GetBlock();
        return block == BlockRegistry.Dirt || block == BlockRegistry.Grass;
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

        if (blockPos == sourceBlockPos.Up() && world.GetBlockAt(sourceBlockPos).GetBlock() == BlockRegistry.Air)
        {
            world.QueueToRemoveBlockAt(blockPos);
        }
    }
}
