using Minecraft.Core.Inventories;
using Minecraft.Core.Worlds.Blocks.States;

namespace Minecraft.Core.Worlds.Blocks.Types;

public sealed class BlockGlass : Block
{
    public BlockGlass(ushort id) : base(id)
    {
        IsOpaque = false;
        HidesFacesAgainstItself = true;
        SecondsToBreak = 0.3F;
    }

    public override ItemStack GetDrop(BlockState blockState) => ItemStack.Empty;

    public override BlockState GetNewDefaultState() => new BlockStateSimple(this);
}
