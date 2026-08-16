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

    // Leaves come away whole, unlike in the game this borrows from, where they tear and leave a sapling
    // behind instead. There are no saplings here and nothing to craft one into, so dropping nothing would
    // not be a trade for something else — it would make a full building block, and the whole canopy of every
    // tree in the world, permanently out of reach.
}
