using Minecraft.Core.Audio;
using Minecraft.Core.Worlds.Blocks.States;

namespace Minecraft.Core.Worlds.Blocks.Types;

public sealed class BlockGravel : Block
{
    public BlockGravel(ushort id) : base(id)
    {
        SoundMaterial = BlockSoundMaterial.Gravel;
    }

    public override BlockState GetNewDefaultState()
    {
        return new BlockStateGravel();
    }
}
