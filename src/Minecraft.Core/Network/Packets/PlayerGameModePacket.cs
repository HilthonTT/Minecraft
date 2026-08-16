using Minecraft.Core.Games;
using Minecraft.Core.IO;
using Minecraft.Core.Network.NetHandler;

namespace Minecraft.Core.Network.Packets;

/// <summary>
/// Tells one client which mode its own player is now in. Sent when <c>/gamemode</c> changes it; the mode a
/// player joins in arrives with the join accept instead, so that there is never a frame in which the client
/// has a world but not the rules it is played by.
/// </summary>
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
