using Minecraft.Core.Audio;
using Minecraft.Core.Worlds.Blocks.States;

namespace Minecraft.Core.Worlds.Blocks.Types;

public sealed class BlockStone : Block
{
    public BlockStone(ushort id) : base(id)
    {
        SoundMaterial = BlockSoundMaterial.Stone;
    }

    public override BlockState GetNewDefaultState()
    {
        return new BlockStateStone();
    }
}
