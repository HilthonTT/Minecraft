using Minecraft.Core.Audio;
using Minecraft.Core.Inventories.Items;
using Minecraft.Core.Worlds.Blocks.States;

namespace Minecraft.Core.Worlds.Blocks.Types;

public sealed class BlockCraftingTable : Block
{
    public BlockCraftingTable(ushort id) : base(id)
    {
        SoundMaterial = BlockSoundMaterial.Wood;
        SecondsToBreak = 2.5F;
        HarvestTool = ToolKind.Axe;
        IsInteractable = true;
    }

    public override BlockState GetNewDefaultState() => new BlockStateSimple(this);
}
