using Minecraft.Core.Audio;
using Minecraft.Core.Worlds.Blocks.States;

namespace Minecraft.Core.Worlds.Blocks.Types;

public sealed class BlockCobblestone : Block
{
    public BlockCobblestone(ushort id) : base(id)
    {
        SoundMaterial = BlockSoundMaterial.Stone;
        SecondsToBreak = 2.0F;
    }

    public override BlockState GetNewDefaultState()
    {
        return new BlockStateCobblestone();
    }
}
