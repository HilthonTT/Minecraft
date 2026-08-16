using Minecraft.Core.IO;
using Minecraft.Core.Network.NetHandler;

namespace Minecraft.Core.Network.Packets;

/// <summary>
/// A player reporting how far they just fell.
/// <para>
/// The only thing a client is trusted to observe about a fall is that one happened and how long it was. The
/// server sees a position every tenth of a second and could not tell a drop from a walk down a staircase
/// without reconstructing the whole flight, while the client has just simulated the body and knows exactly
/// where it left the ground and where it landed. What the fall is worth is still decided on the server, the
/// same way a punch is: the client says what it aimed at, and the server says what it cost.
/// </para>
/// </summary>
public sealed class PlayerFellPacket : Packet
{
    /// <summary>How far the player dropped, in blocks, from the highest point of the fall to the landing.</summary>
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
