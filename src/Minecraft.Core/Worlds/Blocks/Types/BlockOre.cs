using Minecraft.Core.Inventories;
using Minecraft.Core.Inventories.Items;
using Minecraft.Core.Worlds.Blocks.States;

namespace Minecraft.Core.Worlds.Blocks.Types;

/// <summary>
/// A seam of something buried in stone, which comes apart into what was buried rather than into itself.
/// <para>
/// Coal, iron, gold, redstone and diamond all arrive this way, and iron and gold arrive already refined —
/// there is no furnace to smelt them in, and a ladder of tools that stopped at stone because the rung above
/// it needed a block this game does not have would be a ladder with nothing at the top of it. So the ore
/// yields the ingot, and what a furnace would otherwise be for is folded into the pickaxe that has to be good
/// enough to reach it. See DESIGN.md, where this is written down as the deliberate departure it is.
/// </para>
/// <para>
/// The item is taken through a callback rather than held directly, because the ores are built in
/// <see cref="BlockRegistry"/>'s own field initialisers and the items they drop are not registered until
/// after every block is. The same reason <see cref="BlockPlant"/> takes the blocks it may stand on that way.
/// </para>
/// </summary>
public sealed class BlockOre : Block
{
    private readonly Func<Item> _drop;
    private readonly int _count;

    public BlockOre(
        ushort id,
        Func<Item> drop,
        int harvestLevel,
        float secondsToBreak = 2.8F,
        int count = 1) : base(id)
    {
        _drop = drop;
        _count = count;

        SecondsToBreak = secondsToBreak;
        HarvestTool = ToolKind.Pickaxe;
        HarvestLevel = harvestLevel;
        RequiresCorrectTool = true;
    }

    public override ItemStack GetDrop(BlockState blockState) => new(_drop(), _count);

    public override BlockState GetNewDefaultState() => new BlockStateSimple(this);
}
