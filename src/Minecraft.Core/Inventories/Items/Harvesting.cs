using Minecraft.Core.Worlds.Blocks;

namespace Minecraft.Core.Inventories.Items;

public static class Harvesting
{
    public static float SecondsToBreak(Block block, ItemStack held)
    {
        if (!block.IsBreakable)
        {
            return float.PositiveInfinity;
        }

        if (IsCorrectToolFor(block, held))
        {
            return block.SecondsToBreak / held.Tool!.Material.DigSpeed();
        }

        return block.RequiresCorrectTool ? block.SecondsToBreak * WrongToolPenalty : block.SecondsToBreak;
    }

    private const float WrongToolPenalty = 3.33F;

    public static bool CanHarvest(Block block, ItemStack held)
    {
        if (!block.RequiresCorrectTool)
        {
            return true;
        }

        return IsCorrectToolFor(block, held) && held.Tool!.Material.HarvestLevel() >= block.HarvestLevel;
    }

    public static bool IsCorrectToolFor(Block block, ItemStack held) =>
        block.HarvestTool is ToolKind kind && held.Tool is ToolItem tool && tool.Kind == kind;
}
