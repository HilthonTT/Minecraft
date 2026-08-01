using Minecraft.Core.Worlds.Blocks.States;

namespace Minecraft.Core.Worlds.Blocks.Types;

public sealed class BlockGrass : Block
{
    public BlockGrass(ushort id) : base(id)
    {
    }

    public override BlockState GetNewDefaultState()
    {
        return new BlockStateGrass();
    }
}
