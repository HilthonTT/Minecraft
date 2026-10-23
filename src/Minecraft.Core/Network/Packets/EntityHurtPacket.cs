using Minecraft.Core.IO;
using Minecraft.Core.Network.NetHandler;

namespace Minecraft.Core.Network.Packets;

public sealed class EntityHurtPacket : Packet
{
    public int EntityID { get; private set; }

    public bool Died { get; private set; }

    /// <summary>
    /// Who landed the blow, or <see cref="NoAttacker"/>. A swing only wears the weapon once the server has said
    /// it landed: a swing at a mob out of reach, or one still flinching from the last hit, does nothing.
    /// </summary>
    public int AttackerID { get; private set; }

    public const int NoAttacker = -1;

    public EntityHurtPacket(int entityId, bool died, int attackerId = NoAttacker) : base(PacketType.EntityHurt)
    {
        EntityID = entityId;
        Died = died;
        AttackerID = attackerId;
    }

    public override void Process(INetHandler netHandler)
    {
        netHandler.ProcessEntityHurtPacket(this);
    }

    protected override void ToStream(BufferedDataStream bufferedStream)
    {
        bufferedStream.WriteInt32(EntityID);
        bufferedStream.WriteBool(Died);
        bufferedStream.WriteInt32(AttackerID);
    }
}
