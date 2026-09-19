using Minecraft.Core.IO;
using Minecraft.Core.Network.NetHandler;

namespace Minecraft.Core.Network.Packets;

public sealed class PlayerSprintPacket : Packet
{
    public bool IsSprinting { get; private set; }

    public PlayerSprintPacket(bool isSprinting) : base(PacketType.PlayerSprint)
    {
        IsSprinting = isSprinting;
    }

    public override void Process(INetHandler netHandler)
    {
        netHandler.ProcessPlayerSprintPacket(this);
    }

    protected override void ToStream(BufferedDataStream bufferedStream)
    {
        bufferedStream.WriteBool(IsSprinting);
    }
}
