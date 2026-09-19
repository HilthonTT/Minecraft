using Minecraft.Core.Inventories;
using OpenTK.Mathematics;

namespace Minecraft.Core.Worlds.Blocks.Types;

internal static class ContainerSpill
{
    public static void SpillContents(BlockState blockState, World world, Vector3i blockPos)
    {
        if (world is not WorldServer server || blockState is not IContainerState container)
        {
            return;
        }

        for (int slot = 0; slot < container.SlotCount; slot++)
        {
            ItemStack stack = container.GetSlot(slot);
            if (!stack.IsEmpty)
            {
                server.SpawnDroppedItem(blockPos, stack);
            }
        }

        container.ClearContents();
    }
}
