using Minecraft.Core.IO;
using Minecraft.Core.Network.NetHandler;
using OpenTK.Mathematics;

namespace Minecraft.Core.Network.Packets;

/// <summary>
/// Tells the clients that something went off, and where.
/// <para>
/// A blast already reaches them as the hundreds of block removals it leaves behind, but those say nothing
/// about what caused them: they arrive one at a time and look exactly like somebody mining quickly. This is
/// the event itself rather than its aftermath, which is what a client needs to make a bang at the right
/// place — and what anything drawn for it later would need too.
/// </para>
/// </summary>
public sealed class ExplosionPacket : Packet
{
    public Vector3 Position { get; private set; }

    public ExplosionPacket(Vector3 position) : base(PacketType.Explosion)
    {
        Position = position;
    }

    public override void Process(INetHandler netHandler)
    {
        netHandler.ProcessExplosionPacket(this);
    }

    protected override void ToStream(BufferedDataStream bufferedStream)
    {
        bufferedStream.WriteVector3(Position);
    }
}
