using Minecraft.Core.IO;
using Minecraft.Core.Network.NetHandler;

namespace Minecraft.Core.Network.Packets;

public abstract class Packet
{
    protected PacketType _type;

    protected Packet(PacketType type)
    {
        _type = type;
    }

    public void WriteToStream(BufferedDataStream bufferedStream)
    {
        bufferedStream.WriteInt32((int)_type);
        ToStream(bufferedStream);
    }

    public abstract void Process(INetHandler netHandler);

    protected abstract void ToStream(BufferedDataStream bufferedStream);
}
