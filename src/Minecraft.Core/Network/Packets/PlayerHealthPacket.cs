using Minecraft.Core.IO;
using Minecraft.Core.Network.NetHandler;

namespace Minecraft.Core.Network.Packets;

/// <summary>
/// What one player has left, sent to that player alone. The number is the bar to draw;
/// <see cref="WasHurt"/> is what the cry hangs off, since a client cannot tell a blow landing from a half
/// heart mending back by watching the figure change.
/// </summary>
public sealed class PlayerHealthPacket : Packet
{
    public int Health { get; private set; }

    /// <summary>Whether this change was something hitting the player rather than the player mending.</summary>
    public bool WasHurt { get; private set; }

    public PlayerHealthPacket(int health, bool wasHurt) : base(PacketType.PlayerHealth)
    {
        Health = health;
        WasHurt = wasHurt;
    }

    public override void Process(INetHandler netHandler)
    {
        netHandler.ProcessPlayerHealthPacket(this);
    }

    protected override void ToStream(BufferedDataStream bufferedStream)
    {
        bufferedStream.WriteInt32(Health);
        bufferedStream.WriteBool(WasHurt);
    }
}
