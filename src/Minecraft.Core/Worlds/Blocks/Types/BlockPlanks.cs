using Minecraft.Core.Audio;
using Minecraft.Core.Worlds.Blocks.States;

namespace Minecraft.Core.Worlds.Blocks.Types;

public sealed class BlockPlanks : Block
{
    public BlockPlanks(ushort id) : base(id)
    {
        SoundMaterial = BlockSoundMaterial.Wood;
        SecondsToBreak = 1.2F;
    }

    public override BlockState GetNewDefaultState()
    {
        return new BlockStatePlanks();
    }
}
