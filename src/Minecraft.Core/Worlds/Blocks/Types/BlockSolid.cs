using Minecraft.Core.Audio;
using Minecraft.Core.Worlds.Blocks.States;

namespace Minecraft.Core.Worlds.Blocks.Types;

/// <summary>
/// A plain cube that does nothing but sit there: stone, ore, snow and the rest of what the terrain is made
/// of. Anything that has to react to the world around it gets a class of its own instead.
/// <para>
/// A dozen different blocks share this one class, so unlike the rest of them their digging times cannot live
/// in a constructor of their own and are passed in from the registry that names them.
/// </para>
/// </summary>
public sealed class BlockSolid : Block
{
    private readonly bool _dropsItself;

    public BlockSolid(
        ushort id,
        BlockSoundMaterial soundMaterial = BlockSoundMaterial.Stone,
        float secondsToBreak = 1.0F,
        bool dropsItself = true) : base(id)
    {
        SoundMaterial = soundMaterial;
        SecondsToBreak = secondsToBreak;
        _dropsItself = dropsItself;
    }

    public override Block? GetDroppedBlock(BlockState blockState) => _dropsItself ? this : null;

    public override BlockState GetNewDefaultState()
    {
        return new BlockStateSimple(this);
    }
}
