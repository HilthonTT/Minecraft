using Minecraft.Core.Audio;
using Minecraft.Core.Inventories.Items;
using Minecraft.Core.Worlds.Blocks.States;

namespace Minecraft.Core.Worlds.Blocks.Types;

/// <summary>
/// A bench with room to lay nine things out on, which is what a recipe wider than two needs.
/// <para>
/// It holds nothing and remembers nothing: what is laid out on it lives on the client that opened it, and
/// closing the screen hands it all back to whoever was standing there. So the server never hears about this
/// block being reached for at all — the block is interactable so that a right click opens the bench instead
/// of building on top of it, and the opening happens where the inventory it draws from already is.
/// </para>
/// </summary>
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
