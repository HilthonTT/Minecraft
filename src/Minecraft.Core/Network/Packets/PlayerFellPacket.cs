using Minecraft.Core.IO;
using Minecraft.Core.Network.NetHandler;

namespace Minecraft.Core.Network.Packets;

public sealed class PlayerFellPacket : Packet
{
    public float FallenBlocks { get; private set; }

    public PlayerFellPacket(float fallenBlocks) : base(PacketType.PlayerFell)
    {
        FallenBlocks = fallenBlocks;
    }

    public override void Process(INetHandler netHandler)
    {
        netHandler.ProcessPlayerFellPacket(this);
    }

    protected override void ToStream(BufferedDataStream bufferedStream)
    {
        bufferedStream.WriteFloat(FallenBlocks);
    }
}
