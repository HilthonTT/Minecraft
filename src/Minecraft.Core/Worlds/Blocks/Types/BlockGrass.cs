using Minecraft.Core.Audio;
using Minecraft.Core.Worlds.Blocks.States;

namespace Minecraft.Core.Worlds.Blocks.Types;

public sealed class BlockGrass : Block
{
    public BlockGrass(ushort id) : base(id)
    {
        SoundMaterial = BlockSoundMaterial.Grass;
        SecondsToBreak = 0.65F;
    }

    public override BlockState GetNewDefaultState()
    {
        return new BlockStateGrass();
    }

    /// <summary>The green is only the top of it; what is dug up is the dirt underneath.</summary>
    public override Block? GetDroppedBlock(BlockState blockState) => BlockRegistry.Dirt;
}
