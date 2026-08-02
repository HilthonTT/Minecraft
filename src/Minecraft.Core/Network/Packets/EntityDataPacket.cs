using Minecraft.Core.IO;
using Minecraft.Core.Network.NetHandler;
using OpenTK.Mathematics;

namespace Minecraft.Core.Network.Packets;

/// <summary>
/// Where an entity has got to. Sent by a client for the player it controls, and by the server for every
/// entity it owns that a client is being kept up to date on.
/// </summary>
public sealed class EntityDataPacket : Packet
{
    public int EntityID { get; private set; }
    public Vector3 Position { get; private set; }
    public Vector3 Velocity { get; private set; }

    /// <summary>Which way the entity faces, in radians around the Y axis.</summary>
    public float Yaw { get; private set; }

    public EntityDataPacket(int entityId, Vector3 position, Vector3 velocity, float yaw)
        : base(PacketType.EntityPosition)
    {
        EntityID = entityId;
        Position = position;
        Velocity = velocity;
        Yaw = yaw;
    }

    public override void Process(INetHandler netHandler)
    {
        netHandler.ProcessEntityDataPacket(this);
    }

    protected override void ToStream(BufferedDataStream bufferedStream)
    {
        bufferedStream.WriteInt32(EntityID);
        bufferedStream.WriteVector3(Position);
        bufferedStream.WriteVector3(Velocity);
        bufferedStream.WriteFloat(Yaw);
    }
}
