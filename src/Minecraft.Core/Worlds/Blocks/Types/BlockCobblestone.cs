using Minecraft.Core.Worlds.Blocks.States;

namespace Minecraft.Core.Worlds.Blocks.Types;

public sealed class BlockCobblestone : Block
{
    public BlockCobblestone(ushort id) : base(id)
    {
    }

    public override BlockState GetNewDefaultState()
    {
        return new BlockStateCobblestone();
    }
}
