using Minecraft.Core.IO;
using Minecraft.Core.Network.NetHandler;
using OpenTK.Mathematics;

namespace Minecraft.Core.Network.Packets;

public sealed class PlayerRespawnPacket : Packet
{
    public Vector3 SpawnPosition { get; private set; }

    public PlayerRespawnPacket(Vector3 spawnPosition) : base(PacketType.PlayerRespawn)
    {
        SpawnPosition = spawnPosition;
    }

    public override void Process(INetHandler netHandler)
    {
        netHandler.ProcessPlayerRespawnPacket(this);
    }

    protected override void ToStream(BufferedDataStream bufferedStream)
    {
        bufferedStream.WriteVector3(SpawnPosition);
    }
}
