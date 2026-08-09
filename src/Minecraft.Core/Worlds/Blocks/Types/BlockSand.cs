using Minecraft.Core.Audio;
using Minecraft.Core.Worlds.Blocks.States;

namespace Minecraft.Core.Worlds.Blocks.Types;

public sealed class BlockSand : BlockFalling
{
    public BlockSand(ushort id) : base(id)
    {
        SoundMaterial = BlockSoundMaterial.Sand;
    }

    public override BlockState GetNewDefaultState()
    {
        return new BlockStateSand();
    }
}
