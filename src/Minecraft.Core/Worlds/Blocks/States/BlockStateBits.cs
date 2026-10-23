using Minecraft.Core.IO;
using Minecraft.Core.Utilities.Spatial;

namespace Minecraft.Core.Worlds.Blocks.States;

/// <summary>
/// A state whose whole payload is one byte of flags: which half, which way, what it joins on to. Subclasses give
/// the bits names; this keeps the wire and save format of all of them the same single byte.
/// </summary>
public abstract class BlockStateBits(Block block) : BlockState
{
    protected byte _bits;

    public override Block GetBlock() => block;

    /// <summary>Every bit pattern a state can be drawn in, which is what a model keys its built shapes by.</summary>
    public int Variant => _bits;

    public override void ToStream(BufferedDataStream bufferedStream)
    {
        base.ToStream(bufferedStream);
        bufferedStream.WriteByte(_bits);
    }

    public override int PayloadSize() => sizeof(byte);

    public override void ExtractFromByteStream(byte[] bytes, ref int head)
    {
        _bits = (byte)(DataConverter.BytesToByteStruct<byte>(bytes, ref head) & ValidBits);
    }

    /// <summary>The bits this state uses. Anything else in a byte off the wire or the disk is dropped.</summary>
    protected abstract byte ValidBits { get; }

    protected bool GetFlag(int mask) => (_bits & mask) != 0;

    protected void SetFlag(int mask, bool value) =>
        _bits = (byte)(value ? _bits | mask : _bits & ~mask);

    // Facing is kept in the two lowest bits, numbered the way Direction numbers the four horizontal sides.
    protected const int FacingMask = 0b11;

    protected Direction HorizontalFacing
    {
        get => (Direction)(_bits & FacingMask);
        set => _bits = (byte)((_bits & ~FacingMask) | ((byte)(DirectionUtil.IsHorizontal(value) ? value : Direction.Front) & FacingMask));
    }
}
