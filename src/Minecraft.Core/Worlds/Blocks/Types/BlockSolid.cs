using Minecraft.Core.Audio;
using Minecraft.Core.Worlds.Blocks.States;

namespace Minecraft.Core.Worlds.Blocks.Types;

/// <summary>
/// A plain cube that does nothing but sit there: stone, ore, snow and the rest of what the terrain is made
/// of. Anything that has to react to the world around it gets a class of its own instead.
/// </summary>
public sealed class BlockSolid : Block
{
    public BlockSolid(ushort id, BlockSoundMaterial soundMaterial = BlockSoundMaterial.Stone) : base(id)
    {
        SoundMaterial = soundMaterial;
    }

    public override BlockState GetNewDefaultState()
    {
        return new BlockStateSimple(this);
    }
}
