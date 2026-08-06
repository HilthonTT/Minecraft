using Minecraft.Core.Audio;
using Minecraft.Core.Worlds.Blocks.States;

namespace Minecraft.Core.Worlds.Blocks.Types;

public sealed class BlockDirt : Block
{
    public BlockDirt(ushort id) : base(id)
    {
        SoundMaterial = BlockSoundMaterial.Gravel;
    }

    public override BlockState GetNewDefaultState()
    {
        return new BlockStateDirt();
    }
}
