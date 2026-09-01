using Minecraft.Core.Games;
using Minecraft.Core.IO;
using Minecraft.Core.Network.NetHandler;
using OpenTK.Mathematics;

namespace Minecraft.Core.Network.Packets;

public sealed class PlayerJoinAcceptPacket : Packet
{
    public string Name { get; private set; }
    public int PlayerID { get; private set; }
    public Vector3 SpawnPosition { get; private set; }
    public float CurrentTime { get; private set; }

    public GameMode GameMode { get; private set; }

    public int Health { get; private set; }

    public PlayerJoinAcceptPacket(
        string name,
        int playerId,
        Vector3 spawnPosition,
        float currentTime,
        GameMode gameMode,
        int health)
        : base(PacketType.PlayerJoinAccept)
    {
        Name = name;
        PlayerID = playerId;
        SpawnPosition = spawnPosition;
        CurrentTime = currentTime;
        GameMode = gameMode;
        Health = health;
    }

    public override void Process(INetHandler netHandler)
    {
        netHandler.ProcessJoinAcceptPacket(this);
    }

    protected override void ToStream(BufferedDataStream bufferedStream)
    {
        bufferedStream.WriteInt32(PlayerID);
        bufferedStream.WriteUtf8String(Name);
        bufferedStream.WriteVector3(SpawnPosition);
        bufferedStream.WriteFloat(CurrentTime);
        bufferedStream.WriteInt32((int)GameMode);
        bufferedStream.WriteInt32(Health);
    }
}
