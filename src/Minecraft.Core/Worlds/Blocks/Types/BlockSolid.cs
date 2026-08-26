using Minecraft.Core.Audio;
using Minecraft.Core.Inventories;
using Minecraft.Core.Inventories.Items;
using Minecraft.Core.Worlds.Blocks.States;

namespace Minecraft.Core.Worlds.Blocks.Types;

/// <summary>
/// A plain cube that does nothing but sit there: stone, snow and the rest of what the terrain is made of.
/// Anything that has to react to the world around it gets a class of its own instead.
/// <para>
/// A dozen different blocks share this one class, so unlike the rest of them their digging times and what
/// they answer to cannot live in a constructor of their own and are passed in from the registry that names
/// them.
/// </para>
/// </summary>
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
