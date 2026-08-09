using Minecraft.Core.Entities;
using Minecraft.Core.IO;
using Minecraft.Core.Worlds.Blocks;
using Minecraft.Core.Worlds.Chunks;
using OpenTK.Mathematics;

namespace Minecraft.Core.Network.Packets;

public sealed class PacketFactory
{
    public Packet ReadPacket(Session.Session session)
    {
        BinaryReader reader = session.Connection.Reader;

        int packetType = reader.ReadInt32();
        PacketType type = (PacketType)packetType;

        switch (type)
        {
            case PacketType.Chat:
                {
                    string sender = ReadUtf8String(reader);
                    string message = ReadUtf8String(reader);
                    return new ChatPacket(sender, message);
                }
            case PacketType.PlaceBlock:
                {
                    Vector3i blockPos = ReadVector3i(reader);
                    int byteSize = reader.ReadInt32();
                    ushort blockId = reader.ReadUInt16();
                    byte[] bytes = reader.ReadBytes(byteSize);
                    BlockState blockState = BlockRegistry.GetState(BlockRegistry.GetBlockFromIdentifier(blockId));
                    int head = 0;
                    blockState.ExtractFromByteStream(bytes, ref head);
                    return new PlaceBlockPacket(blockState, blockPos);
                }
            case PacketType.RemoveBlock:
                {
                    int head = 0;
                    int byteSize = reader.ReadInt32();
                    byte[] removalBytes = reader.ReadBytes(byteSize);
                    int numOfBlocks = DataConverter.BytesToInt32(removalBytes, ref head);
                    Vector3i[] blockPositions = new Vector3i[numOfBlocks];
                    for (int i = 0; i < numOfBlocks; i++)
                    {
                        blockPositions[i] = new Vector3i(
                            DataConverter.BytesToInt32(removalBytes, ref head),
                            DataConverter.BytesToInt32(removalBytes, ref head),
                            DataConverter.BytesToInt32(removalBytes, ref head));
                    }
                    return new RemoveBlockPacket(blockPositions);
                }
            case PacketType.ChunkData:
                {
                    int head = 0;
                    int chunkByteSize = reader.ReadInt32();
                    byte[] chunkBytes = reader.ReadBytes(chunkByteSize);
                    Worlds.World world = session.Player?.World
                        ?? throw new InvalidOperationException("Received chunk data before joining a world.");
                    Chunk chunk = DataConverter.BytesToChunk(chunkBytes, world, ref head);
                    return new ChunkDataPacket(chunk);
                }
            case PacketType.ChunkUnload:
                {
                    int chunkCount = reader.ReadInt32();
                    List<Vector2> chunksToUnload = new();
                    for (int i = 0; i < chunkCount; i++)
                    {
                        chunksToUnload.Add(new Vector2(reader.ReadInt32(), reader.ReadInt32()));
                    }
                    return new ChunkUnloadPacket(chunksToUnload);
                }
            case PacketType.EntityPosition:
                {
                    int entityId = reader.ReadInt32();
                    Vector3 position = ReadVector3(reader);
                    Vector3 velocity = ReadVector3(reader);
                    float yaw = reader.ReadSingle();
                    return new EntityDataPacket(entityId, position, velocity, yaw);
                }
            case PacketType.EntitySpawn:
                {
                    var entityType = (EntityType)reader.ReadInt32();
                    int entityId = reader.ReadInt32();
                    Vector3 position = ReadVector3(reader);
                    float yaw = reader.ReadSingle();
                    return new EntitySpawnPacket(entityType, entityId, position, yaw);
                }
            case PacketType.EntityDespawn:
                {
                    return new EntityDespawnPacket(reader.ReadInt32());
                }
            case PacketType.PlayerJoinRequest:
                {
                    string playerName = ReadUtf8String(reader);
                    return new PlayerJoinRequestPacket(playerName);
                }
            case PacketType.Explosion:
                {
                    Vector3 position = ReadVector3(reader);
                    return new ExplosionPacket(position);
                }
            case PacketType.PlayerJoinAccept:
                {
                    int playerId = reader.ReadInt32();
                    string playerName = ReadUtf8String(reader);
                    Vector3 position = ReadVector3(reader);
                    float currentTime = reader.ReadSingle();
                    return new PlayerJoinAcceptPacket(playerName, playerId, position, currentTime);
                }
            case PacketType.PlayerJoin:
                {
                    int playerId = reader.ReadInt32();
                    string playerName = ReadUtf8String(reader);
                    return new PlayerJoinPacket(playerName, playerId);
                }
            case PacketType.PlayerLeave:
                {
                    int id = reader.ReadInt32();
                    LeaveReason kickReason = (LeaveReason)reader.ReadInt32();
                    string message = ReadUtf8String(reader);
                    return new PlayerLeavePacket(id, kickReason, message);
                }
            case PacketType.PlayerBlockInteraction:
                {
                    Vector3i blockPos = ReadVector3i(reader);
                    return new PlayerBlockInteractionPacket(blockPos);
                }
            case PacketType.PlayerKeepAlive:
                {
                    return new PlayerKeepAlivePacket();
                }
            case PacketType.PlayerSettings:
                {
                    return new PlayerSettingsPacket(reader.ReadInt32());
                }
            case PacketType.PlayerAttackEntity:
                {
                    return new PlayerAttackEntityPacket(reader.ReadInt32());
                }
            case PacketType.EntityHurt:
                {
                    int entityId = reader.ReadInt32();
                    bool died = reader.ReadBoolean();
                    return new EntityHurtPacket(entityId, died);
                }
            default: throw new Exception("Invalid packet type: " + packetType);
        }
    }

    private static Vector3 ReadVector3(BinaryReader reader) => new(reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle());
    private static Vector3i ReadVector3i(BinaryReader reader) => new(reader.ReadInt32(), reader.ReadInt32(), reader.ReadInt32());

    private string ReadUtf8String(BinaryReader reader)
    {
        int byteCount = reader.ReadInt32();
        byte[] messageBytes = reader.ReadBytes(byteCount);
        return DataConverter.BytesToUtf8String(messageBytes);
    }
}
