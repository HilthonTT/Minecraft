using Minecraft.Core.Worlds.Blocks.States;

namespace Minecraft.Core.Worlds.Blocks.Types;

public sealed class BlockGravel : Block
{
    public BlockGravel(ushort id) : base(id)
    {
    }

    public override BlockState GetNewDefaultState()
    {
        return new BlockStateGravel();
    }
}
