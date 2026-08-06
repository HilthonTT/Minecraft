using Minecraft.Core.Audio;
using Minecraft.Core.Worlds.Blocks.States;

namespace Minecraft.Core.Worlds.Blocks.Types;

public sealed class BlockOakLog : Block
{
    public BlockOakLog(ushort id) : base(id)
    {
        SoundMaterial = BlockSoundMaterial.Wood;
    }

    public override BlockState GetNewDefaultState()
    {
        return new BlockStateOakLog();
    }
}
