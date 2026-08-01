using Minecraft.Core.Entities;
using Minecraft.Core.Entities.Player;
using Minecraft.Core.Games;
using Minecraft.Core.Network.Packets;
using Minecraft.Core.Network.Session;
using Minecraft.Core.Worlds.Blocks;
using Minecraft.Core.Worlds.Chunks;
using Minecraft.Core.Worlds.Generation;
using OpenTK.Mathematics;

namespace Minecraft.Core.Worlds;

/// <summary>
/// The representation of the world used on the server.
/// </summary>
public sealed class WorldServer : World
{
    /// <summary>
    /// All chunks inside a square with side (radius * 2) + 1 at the world origin will not unload.
    /// </summary>
    private const int SpawnAreaRadius = 3;

    private readonly IdTracker _entityIdTracker = new();
    private readonly WorldGenerator _worldGenerator;

    public WorldServer(Game game) : base(game)
    {
        OnBlockPlacedHandler += OnBlockPlacedServer;
        OnBlockRemovedHandler += OnBlockRemovedServer;
        OnEntityDespawnedHandler += OnEntityDespawnedServer;

        _worldGenerator = new WorldGenerator(this);
        LoadSpawnArea();
    }

    private void LoadSpawnArea()
    {
        for (int x = -SpawnAreaRadius; x <= SpawnAreaRadius; x++)
        {
            for (int y = -SpawnAreaRadius; y <= SpawnAreaRadius; y++)
            {
                AddPlayerPresenceToChunk(_worldGenerator.GenerateBlocksForChunkAt(x, y));
            }
        }
    }

    /// <summary>
    /// Creates a suitable spawn position. Destroys blocks if necessary.
    /// </summary>
    public Vector3 GenerateAndGetValidSpawn()
    {
        bool foundSpawn = false;
        Vector3 spawnPosition = Vector3.Zero;

        //Check if there is a suitable position in the middle of the chunk at the origin of the world.
        const int x = 8;
        const int z = 8;
        for (int y = _worldGenerator.SeaLevel; y < Constants.MAX_BUILD_HEIGHT - 3; y++)
        {
            int offset = 0;
            while (GetBlockAt(new Vector3i(x, y + offset, z)).GetBlock() == BlockRegistry.Air && offset < 3)
            {
                offset++;
            }

            if (offset == 3)
            {
                foundSpawn = true;
                spawnPosition = new Vector3(x, y, z);
                break;
            }
        }

        if (foundSpawn)
        {
            return spawnPosition;
        }

        //Create a platform to spawn on
        const int platformSize = 3;
        for (int xOffset = -platformSize; xOffset < platformSize; xOffset++)
        {
            for (int zOffset = -platformSize; zOffset < platformSize; zOffset++)
            {
                QueueToAddBlockAt(new Vector3i(x + xOffset, _worldGenerator.SeaLevel, z + zOffset), BlockRegistry.GetState(BlockRegistry.Stone));
            }
        }

        //Remove the blocks ontop of the platform
        List<Vector3i> toRemoveBlocks = new();
        for (int xOffset = -platformSize; xOffset < platformSize; xOffset++)
        {
            for (int zOffset = -platformSize; zOffset < platformSize; zOffset++)
            {
                for (int yOffset = 1; yOffset <= platformSize; yOffset++)
                {
                    toRemoveBlocks.Add(new Vector3i(x + xOffset, _worldGenerator.SeaLevel + yOffset, z + zOffset));
                }
            }
        }

        QueueToRemoveBlocksAt(toRemoveBlocks);
        ClearBlockRemoveBuffer();
        ClearBlockAddBuffer();

        return new Vector3(x, _worldGenerator.SeaLevel, z);
    }

    public int GenerateEntityId() => _entityIdTracker.GenerateId();

    public void RequestGenerationOfChunk(int playerId, Vector2 gridPosition, Action<GenerateChunkOutput> callback)
    {
        _worldGenerator.AddChunkGenerationRequest(new GenerateChunkRequest()
        {
            PlayerId = playerId,
            GridPosition = gridPosition,
            World = this,
            Callback = callback
        });
    }

    private void OnEntityDespawnedServer(World world, Entity entity)
    {
        _entityIdTracker.ReleaseId(entity.ID);

        if (entity is ServerPlayer)
        {
            Game.Server.BroadcastPacket(new PlayerLeavePacket(entity.ID, LeaveReason.Leave, "disconnect"));
        }
    }

    private void OnBlockPlacedServer(World world, Chunk chunk, Vector3i blockPos, BlockState oldState, BlockState newState)
    {
        foreach (ServerSession session in Game.Server.ConnectedClients)
        {
            if (session.IsBlockPositionInViewRange(blockPos))
            {
                session.WritePacket(new PlaceBlockPacket(newState, blockPos));
            }
        }
    }

    private void OnBlockRemovedServer(World world, Chunk chunk, Vector3i blockPos, BlockState oldState)
    {
        RemoveBlockPacket packet = new(new Vector3i[] { blockPos });
        foreach (ServerSession session in Game.Server.ConnectedClients)
        {
            if (session.IsBlockPositionInViewRange(blockPos))
            {
                session.WritePacket(packet);
            }
        }
    }
}