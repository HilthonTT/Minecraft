using Minecraft.Core.Games;
using Minecraft.Core.IO;
using Minecraft.Core.Network.NetHandler;

namespace Minecraft.Core.Network.Packets;

public sealed class PlayerGameModePacket : Packet
{
    public GameMode GameMode { get; private set; }

    public PlayerGameModePacket(GameMode gameMode) : base(PacketType.PlayerGameMode)
    {
        GameMode = gameMode;
    }

    public override void Process(INetHandler netHandler)
    {
        netHandler.ProcessPlayerGameModePacket(this);
    }

    protected override void ToStream(BufferedDataStream bufferedStream)
    {
        bufferedStream.WriteInt32((int)GameMode);
    }
}
