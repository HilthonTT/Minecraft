using Minecraft.Core.Audio;
using Minecraft.Core.Worlds.Blocks.States;

namespace Minecraft.Core.Worlds.Blocks.Types;

public sealed class BlockStone : Block
{
    public BlockStone(ushort id) : base(id)
    {
        SoundMaterial = BlockSoundMaterial.Stone;
        SecondsToBreak = 1.8F;
    }

    public override BlockState GetNewDefaultState()
    {
        return new BlockStateStone();
    }

    /// <summary>Stone comes apart on the way out, which is where every pile of cobblestone comes from.</summary>
    public override Block? GetDroppedBlock(BlockState blockState) => BlockRegistry.Cobblestone;
}
