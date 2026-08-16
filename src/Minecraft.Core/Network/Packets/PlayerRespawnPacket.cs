using Minecraft.Core.IO;
using Minecraft.Core.Network.NetHandler;
using OpenTK.Mathematics;

namespace Minecraft.Core.Network.Packets;

/// <summary>
/// Puts a player who has just died back at the spawn. The one packet that moves a body the client itself
/// simulates, which is why it is its own thing rather than an ordinary position update: the client refuses
/// those for the entity it owns, and rightly, since accepting them would let the server fight it for control
/// of every step.
/// </summary>
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
