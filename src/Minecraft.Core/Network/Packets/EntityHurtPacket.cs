using Minecraft.Core.IO;
using Minecraft.Core.Network.NetHandler;

namespace Minecraft.Core.Network.Packets;

/// <summary>
/// An entity having been hit, sent to everyone who can see it. What is left of its health is not: nothing on
/// a client shows a number, only that a blow landed, so all a client is told is that one did and whether it
/// was the last.
/// <para>
/// A death still arrives as a despawn of its own a moment later, the way a mob wandering out of range does.
/// This is what gives the client the one thing that despawn cannot say — that the mob was killed — which is
/// the difference between a death sound and a mob quietly ceasing to be tracked.
/// </para>
/// </summary>
public sealed class EntityHurtPacket : Packet
{
    public int EntityID { get; private set; }

    /// <summary>Whether this was the blow that finished it.</summary>
    public bool Died { get; private set; }

    public EntityHurtPacket(int entityId, bool died) : base(PacketType.EntityHurt)
    {
        EntityID = entityId;
        Died = died;
    }

    public override void Process(INetHandler netHandler)
    {
        netHandler.ProcessEntityHurtPacket(this);
    }

    protected override void ToStream(BufferedDataStream bufferedStream)
    {
        bufferedStream.WriteInt32(EntityID);
        bufferedStream.WriteBool(Died);
    }
}
