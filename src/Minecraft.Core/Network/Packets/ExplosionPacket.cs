using Minecraft.Core.IO;
using Minecraft.Core.Network.NetHandler;
using OpenTK.Mathematics;

namespace Minecraft.Core.Network.Packets;

public sealed class ExplosionPacket : Packet
{
    public Vector3 Position { get; private set; }

    public ExplosionPacket(Vector3 position) : base(PacketType.Explosion)
    {
        Position = position;
    }

    public override void Process(INetHandler netHandler)
    {
        netHandler.ProcessExplosionPacket(this);
    }

    protected override void ToStream(BufferedDataStream bufferedStream)
    {
        bufferedStream.WriteVector3(Position);
    }
}
