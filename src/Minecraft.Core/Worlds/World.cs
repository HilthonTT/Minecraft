using Minecraft.Core.Entities;
using Minecraft.Core.Games;
using Minecraft.Core.Logging;
using Minecraft.Core.Utilities;
using Minecraft.Core.Utilities.Vectors;
using Minecraft.Core.Worlds.Blocks;
using Minecraft.Core.Worlds.Chunks;
using OpenTK.Mathematics;
using System.Collections.ObjectModel;

namespace Minecraft.Core.Worlds;

/// <summary>
/// The blocks, chunks and entities that make up a world. Block changes are queued rather than applied
/// immediately, so that a block modifying its neighbours cannot mutate the world part way through a tick.
/// </summary>
public class World
{
    private const float SecondsPerTick = 0.05F;

    private readonly Queue<Vector3i> _toRemoveBlocks = new();
    private readonly Queue<(Vector3i BlockPos, BlockState State)> _toAddBlocks = new();
    private readonly Queue<Entity> _toRemoveEntities = new();

    /// <summary>How many players can currently see each chunk. A chunk unloads when this reaches zero.</summary>
    private readonly Dictionary<Vector2, int> _chunkPlayerPopulation = [];

    private readonly Dictionary<int, Entity> _loadedEntities = [];
    private readonly Dictionary<Vector2, Chunk> _loadedChunks = [];

    private float _elapsedSecondsSinceLastTick;

    public ObjectPool<Chunk> ChunkPool { get; } = new(128);

    public Game Game { get; }

    public Environment Environment { get; }

    public ReadOnlyDictionary<int, Entity> LoadedEntities { get; }

    public ReadOnlyDictionary<Vector2, Chunk> LoadedChunks { get; }

    public delegate void OnBlockPlaced(World world, Chunk chunk, Vector3i blockPos, BlockState oldState, BlockState newState);
    public event OnBlockPlaced? OnBlockPlacedHandler;

    public delegate void OnBlockRemoved(World world, Chunk chunk, Vector3i blockPos, BlockState oldState);
    public event OnBlockRemoved? OnBlockRemovedHandler;

    public delegate void OnChunkLoaded(World world, Chunk chunk);
    public event OnChunkLoaded? OnChunkLoadedHandler;

    public delegate void OnChunkUnloaded(World world, Chunk chunk);
    public event OnChunkUnloaded? OnChunkUnloadedHandler;

    public delegate void OnEntitySpawned(World world, Entity entity);
    public event OnEntitySpawned? OnEntitySpawnedHandler;

    public delegate void OnEntityDespawned(World world, Entity entity);
    public event OnEntityDespawned? OnEntityDespawnedHandler;

    protected World(Game game)
    {
        Game = game;

        LoadedEntities = new ReadOnlyDictionary<int, Entity>(_loadedEntities);
        LoadedChunks = new ReadOnlyDictionary<Vector2, Chunk>(_loadedChunks);

        Environment = new Environment(2400)
        {
            CurrentTime = 1200,
            AmbientColor = new Vector3(0.075F, 0.075F, 0.095F),
        };
    }

    public bool DespawnEntity(int entityId)
    {
        if (!_loadedEntities.Remove(entityId, out Entity? despawnedEntity))
        {
            Logger.Warn("Despawning entity that is not alive with id " + entityId);
            return false;
        }

        despawnedEntity.RaiseOnDespawned();
        OnEntityDespawnedHandler?.Invoke(this, despawnedEntity);
        return true;
    }

    public void SpawnEntity(Entity entity)
    {
        _loadedEntities[entity.ID] = entity;
        OnEntitySpawnedHandler?.Invoke(this, entity);
    }

    public void AddPlayerPresenceToChunk(Chunk chunk)
    {
        var chunkPos = new Vector2(chunk.GridX, chunk.GridZ);
        if (_chunkPlayerPopulation.TryGetValue(chunkPos, out int population))
        {
            _chunkPlayerPopulation[chunkPos] = population + 1;
            return;
        }

        _chunkPlayerPopulation.Add(chunkPos, 1);
        LoadChunk(chunk);
    }

    public bool RemovePlayerPresenceOfChunk(Chunk chunk)
    {
        var chunkPos = new Vector2(chunk.GridX, chunk.GridZ);
        if (!_chunkPlayerPopulation.TryGetValue(chunkPos, out int population))
        {
            Logger.Warn("Chunk with no player population count: " + chunkPos);
            return false;
        }

        int newPopulation = population - 1;
        if (newPopulation > 0)
        {
            _chunkPlayerPopulation[chunkPos] = newPopulation;
            return true;
        }

        _chunkPlayerPopulation.Remove(chunkPos);
        return UnloadChunk(chunk);
    }

    protected void LoadChunk(Chunk chunk)
    {
        var chunkPos = new Vector2(chunk.GridX, chunk.GridZ);
        if (_loadedChunks.ContainsKey(chunkPos))
        {
            Logger.Warn("World " + GetType() + " already had chunk data for " + chunkPos);
            return;
        }

        _loadedChunks.Add(chunkPos, chunk);
        OnChunkLoadedHandler?.Invoke(this, chunk);
    }

    protected bool UnloadChunk(Chunk chunk)
    {
        var chunkPos = new Vector2(chunk.GridX, chunk.GridZ);
        if (!_loadedChunks.Remove(chunkPos))
        {
            return false;
        }

        OnChunkUnloadedHandler?.Invoke(this, chunk);
        OnChunkUnloadedPostProcess(chunk);
        return true;
    }

    protected virtual void OnChunkUnloadedPostProcess(Chunk chunk)
    {
        ChunkPool.ReturnObject(chunk);
    }

    public virtual void Update(float deltaTimeSeconds)
    {
        foreach (Entity entity in _loadedEntities.Values)
        {
            entity.Update(deltaTimeSeconds, this);
        }

        Tick(deltaTimeSeconds);

        Environment.Update(deltaTimeSeconds);

        ClearBlockRemoveBuffer();
        ClearBlockAddBuffer();
        ClearEntityRemoveBuffer();
    }

    protected void ClearBlockRemoveBuffer()
    {
        while (_toRemoveBlocks.Count > 0)
        {
            RemoveBlockAt(_toRemoveBlocks.Dequeue());
        }
    }

    protected void ClearBlockAddBuffer()
    {
        while (_toAddBlocks.Count > 0)
        {
            (Vector3i blockPos, BlockState state) = _toAddBlocks.Dequeue();
            AddBlockToWorld(blockPos, state);
        }
    }

    protected void ClearEntityRemoveBuffer()
    {
        while (_toRemoveEntities.Count > 0)
        {
            _loadedEntities.Remove(_toRemoveEntities.Dequeue().ID);
        }
    }

    private void Tick(float deltaTime)
    {
        _elapsedSecondsSinceLastTick += deltaTime;
        if (_elapsedSecondsSinceLastTick < SecondsPerTick)
        {
            return;
        }

        // Ticking a block can queue chunk loads, so the collections are copied before being walked.
        foreach (Chunk chunk in _loadedChunks.Values.ToArray())
        {
            chunk.Tick(_elapsedSecondsSinceLastTick, this);
        }

        foreach (Entity entity in _loadedEntities.Values.ToArray())
        {
            entity.Tick(_elapsedSecondsSinceLastTick, this);
        }

        _elapsedSecondsSinceLastTick = 0;
    }

    /// <summary>The grid position of the chunk containing the given world coordinates.</summary>
    public static Vector2 GetChunkPosition(float worldX, float worldZ)
    {
        return new Vector2(
            (int)MathF.Floor(worldX / 16),
            (int)MathF.Floor(worldZ / 16));
    }

    public void QueueToRemoveBlockAt(Vector3i blockPos)
    {
        _toRemoveBlocks.Enqueue(blockPos);
    }

    public void QueueToRemoveBlocksAt(IEnumerable<Vector3i> blockPositions)
    {
        foreach (Vector3i blockPos in blockPositions)
        {
            _toRemoveBlocks.Enqueue(blockPos);
        }
    }

    public void QueueToAddBlockAt(Vector3i blockPos, BlockState block)
    {
        _toAddBlocks.Enqueue((blockPos, block));
    }

    private bool RemoveBlockAt(Vector3i blockPos)
    {
        if (IsOutsideBuildHeight(blockPos.Y))
        {
            Logger.Warn("Tried to remove block outside of building height.");
            return false;
        }

        Vector2 chunkPos = GetChunkPosition(blockPos.X, blockPos.Z);
        if (!_loadedChunks.TryGetValue(chunkPos, out Chunk? chunk))
        {
            Logger.Warn("Tried to remove block in chunk that is not loaded.");
            return false;
        }

        BlockState oldState = GetBlockAt(blockPos);
        if (oldState.GetBlock() == BlockRegistry.Air)
        {
            return false;
        }

        Vector3i chunkLocalPos = blockPos.ToChunkLocal();
        chunk.RemoveBlockAt(chunkLocalPos.X, chunkLocalPos.Y, chunkLocalPos.Z);
        oldState.GetBlock().OnDestroy(oldState, this, blockPos);
        OnBlockRemovedHandler?.Invoke(this, chunk, blockPos, oldState);
        return true;
    }

    private bool AddBlockToWorld(Vector3i blockPos, BlockState newBlockState)
    {
        Vector2 chunkPos = GetChunkPosition(blockPos.X, blockPos.Z);
        if (!_loadedChunks.TryGetValue(chunkPos, out Chunk? chunk))
        {
            Logger.Warn("Tried to place block in chunk that is not loaded.");
            return false;
        }

        BlockState oldState = GetBlockAt(blockPos);

        // Only the server validates placement. The client trusts what it is told, since the server has
        // already checked it and the client may not have the surrounding blocks loaded.
        if (this is WorldServer && !CanPlaceBlockAt(blockPos, oldState, newBlockState))
        {
            return false;
        }

        Vector3i chunkLocalPos = blockPos.ToChunkLocal();
        chunk.AddBlockAt(chunkLocalPos.X, chunkLocalPos.Y, chunkLocalPos.Z, newBlockState);
        newBlockState.GetBlock().OnAdd(newBlockState, this, blockPos);
        OnBlockPlacedHandler?.Invoke(this, chunk, blockPos, oldState, newBlockState);

        return true;
    }

    private bool CanPlaceBlockAt(Vector3i blockPos, BlockState oldState, BlockState newBlockState)
    {
        if (newBlockState.GetBlock() == BlockRegistry.Air)
        {
            Logger.Warn("Tried to place air. Remove the block instead.");
            return false;
        }

        if (IsOutsideBuildHeight(blockPos.Y))
        {
            Logger.Warn("Tried to place block outside of building height.");
            return false;
        }

        if (oldState.GetBlock() != BlockRegistry.Air && !oldState.GetBlock().IsOverridable)
        {
            Logger.Warn("Tried to place block where there was already one.");
            return false;
        }

        foreach (Entity entity in _loadedEntities.Values)
        {
            if (newBlockState.GetBlock()
                .GetCollisionBox(newBlockState, blockPos)
                .Any(aabb => entity.Hitbox.Intersects(aabb)))
            {
                Logger.Warn("Tried to place a block inside an entity.");
                return false;
            }
        }

        return true;
    }

    public bool IsOutsideBuildHeight(int worldY)
    {
        return worldY < 0 || worldY >= Constants.MAX_BUILD_HEIGHT;
    }

    /// <summary>
    /// The block at the given world position, or air when the position lies in a chunk that is not loaded.
    /// </summary>
    public BlockState GetBlockAt(Vector3i blockPos)
    {
        Vector2 chunkPos = GetChunkPosition(blockPos.X, blockPos.Z);
        if (!_loadedChunks.TryGetValue(chunkPos, out Chunk? chunk))
        {
            return BlockRegistry.GetState(BlockRegistry.Air);
        }

        return chunk.GetBlockAt(blockPos.ToChunkLocal());
    }

    /// <summary>The loaded chunks diagonally adjacent to the given one.</summary>
    public List<Chunk> GetCardinalChunks(Chunk chunk)
    {
        List<Chunk> chunks = [];

        ReadOnlySpan<Vector2> offsets =
        [
            new(-1, -1),
            new(-1, 1),
            new(1, 1),
            new(1, -1),
        ];

        foreach (Vector2 offset in offsets)
        {
            if (_loadedChunks.TryGetValue(new Vector2(chunk.GridX + offset.X, chunk.GridZ + offset.Y), out Chunk? neighbour))
            {
                chunks.Add(neighbour);
            }
        }

        return chunks;
    }
}
