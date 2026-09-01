using Minecraft.Core.Audio;
using Minecraft.Core.Inventories;
using Minecraft.Core.Inventories.Items;
using Minecraft.Core.Worlds.Blocks.States;

namespace Minecraft.Core.Worlds.Blocks.Types;

public sealed class BlockSolid : Block
{
    private readonly bool _dropsItself;

    public BlockSolid(
        ushort id,
        BlockSoundMaterial soundMaterial = BlockSoundMaterial.Stone,
        float secondsToBreak = 1.0F,
        bool dropsItself = true,
        ToolKind? harvestTool = null,
        bool requiresCorrectTool = false) : base(id)
    {
        SoundMaterial = soundMaterial;
        SecondsToBreak = secondsToBreak;
        HarvestTool = harvestTool;
        RequiresCorrectTool = requiresCorrectTool;
        _dropsItself = dropsItself;
    }

    public override ItemStack GetDrop(BlockState blockState) =>
        _dropsItself ? new ItemStack(this, 1) : ItemStack.Empty;

    public override BlockState GetNewDefaultState()
    {
        return new BlockStateSimple(this);
    }
}
