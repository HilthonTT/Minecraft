using Minecraft.Core.IO;
using Minecraft.Core.Network.NetHandler;

namespace Minecraft.Core.Network.Packets;

/// <summary>
/// What a client has asked of the server on its own behalf. Only the view distance so far, which the server
/// needs because it is the side that decides which chunks are streamed and kept loaded.
/// <para>
/// Sent on joining and again whenever the player changes it, so the world being streamed follows the slider
/// rather than waiting for the next session.
/// </para>
/// </summary>
public sealed class PlayerSettingsPacket : Packet
{
    public int ViewDistance { get; private set; }

    public PlayerSettingsPacket(int viewDistance) : base(PacketType.PlayerSettings)
    {
        ViewDistance = viewDistance;
    }

    public override void Process(INetHandler netHandler)
    {
        netHandler.ProcessPlayerSettingsPacket(this);
    }

    protected override void ToStream(BufferedDataStream bufferedStream)
    {
        bufferedStream.WriteInt32(ViewDistance);
    }
}
