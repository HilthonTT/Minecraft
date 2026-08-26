using Minecraft.Core.Audio;
using Minecraft.Core.Inventories.Items;
using Minecraft.Core.Worlds.Blocks.States;

namespace Minecraft.Core.Worlds.Blocks.Types;

public sealed class BlockSand : BlockFalling
{
    public BlockSand(ushort id) : base(id)
    {
        SoundMaterial = BlockSoundMaterial.Sand;
        SecondsToBreak = 0.55F;
        HarvestTool = ToolKind.Shovel;
    }

    public override BlockState GetNewDefaultState()
    {
        return new BlockStateSand();
    }
}
