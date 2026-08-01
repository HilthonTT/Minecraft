using Minecraft.Core.IO;

namespace Minecraft.Core.Worlds.Blocks;

public abstract class BlockState
{
    public abstract Block GetBlock();

    public virtual void ToStream(BufferedDataStream bufferedStream)
    {
        bufferedStream.WriteUInt16(GetBlock().Id);
    }

    public virtual int PayloadSize() => 0;

    public virtual void ExtractFromByteStream(byte[] bytes, ref int head) { }

    public override string ToString() => GetType().ToString();
}
