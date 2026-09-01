using Minecraft.Core.IO;
using Minecraft.Core.Network.NetHandler;

namespace Minecraft.Core.Network.Packets;

public sealed class PlayerHealthPacket : Packet
{
    public int Health { get; private set; }

    public bool WasHurt { get; private set; }

    public PlayerHealthPacket(int health, bool wasHurt) : base(PacketType.PlayerHealth)
    {
        Health = health;
        WasHurt = wasHurt;
    }

    public override void Process(INetHandler netHandler)
    {
        netHandler.ProcessPlayerHealthPacket(this);
    }

    protected override void ToStream(BufferedDataStream bufferedStream)
    {
        bufferedStream.WriteInt32(Health);
        bufferedStream.WriteBool(WasHurt);
    }
}
