using Minecraft.Core.Audio;
using Minecraft.Core.Inventories.Items;
using Minecraft.Core.Worlds.Blocks.States;
using OpenTK.Mathematics;

namespace Minecraft.Core.Worlds.Blocks.Types;

public sealed class BlockChest : Block
{
    public BlockChest(ushort id) : base(id)
    {
        SoundMaterial = BlockSoundMaterial.Wood;
        SecondsToBreak = 2.5F;
        HarvestTool = ToolKind.Axe;
        HasCustomState = true;
    }

    public override BlockState GetNewDefaultState() => new BlockStateChest();

    public override void OnDestroy(BlockState blockState, World world, Vector3i blockPos)
    {
        ContainerSpill.SpillContents(blockState, world, blockPos);
        base.OnDestroy(blockState, world, blockPos);
    }
}
