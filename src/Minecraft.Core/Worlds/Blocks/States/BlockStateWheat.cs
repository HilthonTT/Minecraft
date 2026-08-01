using Minecraft.Core.IO;

namespace Minecraft.Core.Worlds.Blocks.States;

public sealed class BlockStateWheat : BlockState
{
    public ushort Maturity { get; set; }

    public float ElapsedTimeSinceLastGrowth { get; set; }

    public override Block GetBlock()
    {
        return BlockRegistry.Wheat;
    }

    public override void ToStream(BufferedDataStream bufferedStream)
    {
        base.ToStream(bufferedStream);
        bufferedStream.WriteUInt16(Maturity);
        bufferedStream.WriteFloat(ElapsedTimeSinceLastGrowth);
    }

    public override int PayloadSize() => sizeof(ushort) + sizeof(float);

    public override void ExtractFromByteStream(byte[] bytes, ref int head)
    {
        Maturity = DataConverter.BytesToUInt16(bytes, ref head);
        ElapsedTimeSinceLastGrowth = DataConverter.BytesToFloat(bytes, ref head);
    }
}
