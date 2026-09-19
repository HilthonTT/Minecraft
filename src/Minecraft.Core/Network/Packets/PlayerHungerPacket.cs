using Minecraft.Core.IO;
using Minecraft.Core.Network.NetHandler;

namespace Minecraft.Core.Network.Packets;

public sealed class PlayerHungerPacket : Packet
{
    public int Food { get; private set; }

    public PlayerHungerPacket(int food) : base(PacketType.PlayerHunger)
    {
        Food = food;
    }

    public override void Process(INetHandler netHandler)
    {
        netHandler.ProcessPlayerHungerPacket(this);
    }

    protected override void ToStream(BufferedDataStream bufferedStream)
    {
        bufferedStream.WriteInt32(Food);
    }
}
