using Minecraft.Core.Inventories;

namespace Minecraft.Core.Worlds.Blocks;

public interface IContainerState
{
    int SlotCount { get; }

    ItemStack GetSlot(int slot);

    void SetSlot(int slot, ItemStack stack);

    bool Accepts(int slot, ItemStack stack);

    bool IsTakeOnly(int slot);

    void ClearContents();

    void CopyContentsFrom(BlockState source);
}
