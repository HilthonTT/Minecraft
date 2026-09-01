using Minecraft.Core.Inventories;
using Minecraft.Core.Inventories.Items;
using Minecraft.Core.Worlds.Blocks.States;

namespace Minecraft.Core.Worlds.Blocks.Types;

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
