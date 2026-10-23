using Minecraft.Core.Audio;
using Minecraft.Core.Inventories;
using Minecraft.Core.Inventories.Items;
using Minecraft.Core.Utilities.Vectors;
using Minecraft.Core.Worlds.Blocks.States;
using OpenTK.Mathematics;

namespace Minecraft.Core.Worlds.Blocks.Types;

public sealed class BlockFarmland : Block
{
    public BlockFarmland(ushort id) : base(id)
    {
        SoundMaterial = BlockSoundMaterial.Gravel;
        SecondsToBreak = 0.6F;
        HarvestTool = ToolKind.Shovel;
    }

    public override BlockState GetNewDefaultState() => new BlockStateSimple(this);

    public override ItemStack GetDrop(BlockState blockState) => new(BlockRegistry.Dirt, 1);

    public override void OnNotify(
        BlockState blockState,
        BlockState sourceBlockState,
        World world,
        Vector3i blockPos,
        Vector3i sourceBlockPos)
    {
        if (world is not WorldServer || sourceBlockPos != blockPos.Up())
        {
            return;
        }

        // Tilled earth with something solid laid over it is just dirt again.
        if (world.GetBlockAt(sourceBlockPos).GetBlock().IsOpaque)
        {
            world.QueueToRemoveBlockAt(blockPos);
            world.QueueToAddBlockAt(blockPos, BlockRegistry.GetState(BlockRegistry.Dirt));
        }
    }
}
