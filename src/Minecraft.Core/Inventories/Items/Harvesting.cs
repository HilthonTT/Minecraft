using Minecraft.Core.Worlds.Blocks;

namespace Minecraft.Core.Inventories.Items;

/// <summary>
/// The two questions a swing at a block asks of whatever is in the hand: how long it will take, and whether
/// anything will be left behind for it.
/// <para>
/// Both are answered here rather than on either side of the pair, because each is a fact about the meeting of
/// a block and a tool and belongs to neither alone. The client asks the first on every frame it is digging,
/// and both sides ask the second — the client so that a swing which earns nothing still feels like the swing
/// it is, and the server because the server is what decides whether anything drops.
/// </para>
/// </summary>
public static class Harvesting
{
    /// <summary>
    /// How long this block takes to break with this in hand. A block's bare handed time divided by the dig
    /// speed of the tool, when the tool is the right kind for it; the plain time otherwise, since a shovel
    /// swung at stone is worth no more than a fist.
    /// <para>
    /// Blocks that need a tool and are being dug without one take longer still. Without that, the fastest way
    /// through a wall of stone would be to throw the pickaxe away and keep the cobblestone you were not going
    /// to get either way.
    /// </para>
    /// </summary>
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

    /// <summary>
    /// How much longer a block that wants a tool takes without one. Enough to be felt across a wall of stone
    /// and not so much that a player who has lost their pickaxe is walled in by it.
    /// </summary>
    private const float WrongToolPenalty = 3.33F;

    /// <summary>
    /// Whether breaking this block with this in hand leaves anything behind. A block that asks for no tool
    /// always does; one that asks for a tool wants the right kind of it, made of something that reaches at
    /// least as deep as the block is buried.
    /// </summary>
    public static bool CanHarvest(Block block, ItemStack held)
    {
        if (!block.RequiresCorrectTool)
        {
            return true;
        }

        return IsCorrectToolFor(block, held) && held.Tool!.Material.HarvestLevel() >= block.HarvestLevel;
    }

    /// <summary>
    /// Whether what is held is the kind of tool this block answers to. Says nothing about what it is made of;
    /// that is the level, and the two are asked separately because they decide different things — the kind
    /// decides how fast, and the material decides whether at all.
    /// </summary>
    public static bool IsCorrectToolFor(Block block, ItemStack held) =>
        block.HarvestTool is ToolKind kind && held.Tool is ToolItem tool && tool.Kind == kind;
}
