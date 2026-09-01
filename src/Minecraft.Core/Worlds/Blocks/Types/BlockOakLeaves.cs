using Minecraft.Core.Audio;
using Minecraft.Core.Worlds.Blocks.States;

namespace Minecraft.Core.Worlds.Blocks.Types;

public sealed class BlockOakLeaves : Block
{
    public BlockOakLeaves(ushort id) : base(id)
    {
        SoundMaterial = BlockSoundMaterial.Grass;
        SecondsToBreak = 0.2F;
    }

    public override BlockState GetNewDefaultState()
    {
        return new BlockStateOakLeaves();
    }

}
