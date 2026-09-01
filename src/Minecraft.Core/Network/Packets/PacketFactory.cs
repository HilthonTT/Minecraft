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
        var type = (PacketType)packetType;

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
                int byteSize = ReadLength(reader, MaxBlockStateBytes, "A block state");
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
                int byteSize = ReadLength(reader, MaxPayloadBytes, "A block removal");
                byte[] removalBytes = reader.ReadBytes(byteSize);
                int numOfBlocks = DataConverter.BytesToInt32(removalBytes, ref head);

                if (numOfBlocks < 0 || (long)numOfBlocks * 3 * sizeof(int) > removalBytes.Length - head)
                {
                    throw new InvalidDataException(
                        $"A block removal claims {numOfBlocks} positions, which {removalBytes.Length} bytes "
                        + "cannot hold.");
                }

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
                int chunkByteSize = ReadLength(reader, MaxPayloadBytes, "Chunk data");
                byte[] chunkBytes = reader.ReadBytes(chunkByteSize);
                Worlds.World world = session.Player?.World
                    ?? throw new InvalidOperationException("Received chunk data before joining a world.");
                Chunk chunk = DataConverter.BytesToChunk(chunkBytes, world, ref head);
                return new ChunkDataPacket(chunk);
            }
            case PacketType.ChunkUnload:
            {
                int chunkCount = ReadLength(reader, MaxChunkUnloadCount, "A chunk unload");
                List<Vector2> chunksToUnload = new(chunkCount);
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
                var gameMode = (Games.GameMode)reader.ReadInt32();
                int health = reader.ReadInt32();
                return new PlayerJoinAcceptPacket(playerName, playerId, position, currentTime, gameMode, health);
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
            case PacketType.PlayerGameMode:
            {
                return new PlayerGameModePacket((Games.GameMode)reader.ReadInt32());
            }
            case PacketType.PlayerHealth:
            {
                int health = reader.ReadInt32();
                bool wasHurt = reader.ReadBoolean();
                return new PlayerHealthPacket(health, wasHurt);
            }
            case PacketType.PlayerRespawn:
            {
                return new PlayerRespawnPacket(ReadVector3(reader));
            }
            case PacketType.PlayerFell:
            {
                return new PlayerFellPacket(reader.ReadSingle());
            }
            case PacketType.ItemSpawn:
            {
                int entityId = reader.ReadInt32();
                Vector3 position = ReadVector3(reader);
                ushort itemId = reader.ReadUInt16();
                int count = reader.ReadInt32();
                int damage = reader.ReadInt32();
                return new ItemSpawnPacket(entityId, position, itemId, count, damage);
            }
            case PacketType.ItemPickup:
            {
                int entityId = reader.ReadInt32();
                ushort itemId = reader.ReadUInt16();
                int count = reader.ReadInt32();
                int damage = reader.ReadInt32();
                return new ItemPickupPacket(entityId, itemId, count, damage);
            }
            case PacketType.PlayerDropItem:
            {
                ushort itemId = reader.ReadUInt16();
                int count = reader.ReadInt32();
                int damage = reader.ReadInt32();
                return new PlayerDropItemPacket(itemId, count, damage);
            }
            case PacketType.PlayerHeldItem:
            {
                ushort itemId = reader.ReadUInt16();
                int damage = reader.ReadInt32();
                return new PlayerHeldItemPacket(itemId, damage);
            }
            default:
                throw new Exception("Invalid packet type: " + packetType);
        }
    }

    private const int MaxPayloadBytes = 4 * 1024 * 1024;

    private const int MaxStringBytes = 64 * 1024;

    private const int MaxBlockStateBytes = 4 * 1024;

    private const int MaxChunkUnloadCount = 64 * 1024;

    private static Vector3 ReadVector3(BinaryReader reader) => new(reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle());
    private static Vector3i ReadVector3i(BinaryReader reader) => new(reader.ReadInt32(), reader.ReadInt32(), reader.ReadInt32());

    private static int ReadLength(BinaryReader reader, int maximum, string what)
    {
        int length = reader.ReadInt32();
        if (length < 0 || length > maximum)
        {
            throw new InvalidDataException(
                $"{what} claims a length of {length}, which is outside 0 to {maximum}.");
        }

        return length;
    }

    private static string ReadUtf8String(BinaryReader reader)
    {
        int byteCount = ReadLength(reader, MaxStringBytes, "A string");
        byte[] messageBytes = reader.ReadBytes(byteCount);
        return DataConverter.BytesToUtf8String(messageBytes);
    }
}
