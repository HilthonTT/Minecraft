using Minecraft.Core.Audio;
using Minecraft.Core.Inventories;
using Minecraft.Core.Inventories.Items;
using Minecraft.Core.Worlds.Blocks.States;

namespace Minecraft.Core.Worlds.Blocks.Types;

public sealed class BlockStone : Block
{
    public BlockStone(ushort id) : base(id)
    {
        SoundMaterial = BlockSoundMaterial.Stone;
        SecondsToBreak = 1.8F;
        HarvestTool = ToolKind.Pickaxe;
        RequiresCorrectTool = true;
    }

    public override BlockState GetNewDefaultState()
    {
        return new BlockStateStone();
    }

    /// <summary>Stone comes apart on the way out, which is where every pile of cobblestone comes from.</summary>
    public override ItemStack GetDrop(BlockState blockState) => new(BlockRegistry.Cobblestone, 1);
}
