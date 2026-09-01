using Minecraft.Core.Entities;
using Minecraft.Core.IO;
using Minecraft.Core.Network.NetHandler;
using OpenTK.Mathematics;

namespace Minecraft.Core.Network.Packets;

public sealed class EntitySpawnPacket : Packet
{
    public EntityType EntityType { get; private set; }
    public int EntityID { get; private set; }
    public Vector3 Position { get; private set; }
    public float Yaw { get; private set; }

    public EntitySpawnPacket(EntityType entityType, int entityId, Vector3 position, float yaw)
        : base(PacketType.EntitySpawn)
    {
        EntityType = entityType;
        EntityID = entityId;
        Position = position;
        Yaw = yaw;
    }

    public override void Process(INetHandler netHandler)
    {
        netHandler.ProcessEntitySpawnPacket(this);
    }

    protected override void ToStream(BufferedDataStream bufferedStream)
    {
        bufferedStream.WriteInt32((int)EntityType);
        bufferedStream.WriteInt32(EntityID);
        bufferedStream.WriteVector3(Position);
        bufferedStream.WriteFloat(Yaw);
    }
}
