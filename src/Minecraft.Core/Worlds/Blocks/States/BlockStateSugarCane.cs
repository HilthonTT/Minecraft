using Minecraft.Core.IO;

namespace Minecraft.Core.Worlds.Blocks.States;

public sealed class BlockStateSugarCane : BlockState
{
    public float ElapsedTimeSinceLastGrowth { get; set; }

    public override Block GetBlock()
    {
        return BlockRegistry.SugarCane;
    }

    public override void ToStream(BufferedDataStream bufferedStream)
    {
        base.ToStream(bufferedStream);
        bufferedStream.WriteFloat(ElapsedTimeSinceLastGrowth);
    }

    public override int PayloadSize() => sizeof(float);

    public override void ExtractFromByteStream(byte[] bytes, ref int head)
    {
        ElapsedTimeSinceLastGrowth = DataConverter.BytesToFloat(bytes, ref head);
    }
}
