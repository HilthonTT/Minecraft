using Minecraft.Core.Audio;
using Minecraft.Core.Worlds.Blocks.States;

namespace Minecraft.Core.Worlds.Blocks.Types;

public sealed class BlockSandstone : Block
{
    public BlockSandstone(ushort id) : base(id)
    {
        SoundMaterial = BlockSoundMaterial.Stone;
        SecondsToBreak = 1.6F;
    }

    public override BlockState GetNewDefaultState()
    {
        return new BlockStateSandstone();
    }
}
