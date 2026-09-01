using Minecraft.Core.Audio;
using Minecraft.Core.Inventories;
using Minecraft.Core.Inventories.Items;
using Minecraft.Core.Worlds.Blocks.States;

namespace Minecraft.Core.Worlds.Blocks.Types;

public sealed class BlockGrass : Block
{
    public BlockGrass(ushort id) : base(id)
    {
        SoundMaterial = BlockSoundMaterial.Grass;
        SecondsToBreak = 0.65F;
        HarvestTool = ToolKind.Shovel;
    }

    public override BlockState GetNewDefaultState()
    {
        return new BlockStateGrass();
    }

    public override ItemStack GetDrop(BlockState blockState) => new(BlockRegistry.Dirt, 1);
}
