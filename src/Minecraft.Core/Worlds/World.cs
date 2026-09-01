using Minecraft.Core.Entities;
using Minecraft.Core.Games;
using Minecraft.Core.Logging;
using Minecraft.Core.Physics;
using Minecraft.Core.Utilities;
using Minecraft.Core.Utilities.Vectors;
using Minecraft.Core.Worlds.Blocks;
using Minecraft.Core.Worlds.Chunks;
using OpenTK.Mathematics;
using System.Collections.ObjectModel;

namespace Minecraft.Core.Worlds;

public class World
{
    private const float SecondsPerTick = 0.05F;

    private readonly Queue<Vector3i> _toRemoveBlocks = new();
    private readonly Queue<(Vector3i BlockPos, BlockState State)> _toAddBlocks = new();
    private readonly Queue<Entity> _toRemoveEntities = new();

    private readonly Dictionary<Vector2, int> _chunkPlayerPopulation = [];

    private readonly Dictionary<int, Entity> _loadedEntities = [];
    private readonly Dictionary<Vector2, Chunk> _loadedChunks = [];

    private readonly Dictionary<Vector3i, long> _scheduledBlockUpdates = [];

    private readonly List<Vector3i> _dueBlockUpdates = [];

    private float _elapsedSecondsSinceLastTick;
    private long _tickCount;

    public const int DayLengthSeconds = 2400;

    public const float MiddayTimeSeconds = DayLengthSeconds / 2F;

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

        Environment = new Environment(DayLengthSeconds)
        {
            CurrentTime = MiddayTimeSeconds,
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

    protected virtual void OnTick(float deltaTime)
    {
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

        _tickCount++;

        foreach (Chunk chunk in _loadedChunks.Values.ToArray())
        {
            chunk.Tick(_elapsedSecondsSinceLastTick, this);
        }

        RunScheduledBlockUpdates();

        foreach (Entity entity in _loadedEntities.Values.ToArray())
        {
            entity.Tick(_elapsedSecondsSinceLastTick, this);
        }

        OnTick(_elapsedSecondsSinceLastTick);

        _elapsedSecondsSinceLastTick = 0;
    }

    public void ScheduleBlockUpdate(Vector3i blockPos, int delayTicks)
    {
        if (this is not WorldServer)
        {
            return;
        }

        long dueTick = _tickCount + Math.Max(delayTicks, 1);
        if (_scheduledBlockUpdates.TryGetValue(blockPos, out long pendingTick) && pendingTick <= dueTick)
        {
            return;
        }

        _scheduledBlockUpdates[blockPos] = dueTick;
    }

    private void RunScheduledBlockUpdates()
    {
        if (_scheduledBlockUpdates.Count == 0)
        {
            return;
        }

        _dueBlockUpdates.Clear();

        foreach (KeyValuePair<Vector3i, long> pending in _scheduledBlockUpdates)
        {
            if (pending.Value <= _tickCount)
            {
                _dueBlockUpdates.Add(pending.Key);
            }
        }

        foreach (Vector3i blockPos in _dueBlockUpdates)
        {
            _scheduledBlockUpdates.Remove(blockPos);
        }

        foreach (Vector3i blockPos in _dueBlockUpdates)
        {
            BlockState state = GetBlockAt(blockPos);
            state.GetBlock().OnScheduledUpdate(state, this, blockPos);
        }
    }

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

        if (IsBlockedByEntity(blockPos, newBlockState))
        {
            Logger.Warn("Tried to place a block inside an entity.");
            return false;
        }

        return true;
    }

    public bool IsBlockedByEntity(Vector3i blockPos, BlockState blockState)
    {
        AxisAlignedBox[] collisionBoxes = blockState.GetBlock().GetCollisionBox(blockState, blockPos);
        if (collisionBoxes.Length == 0)
        {
            return false;
        }

        foreach (Entity entity in _loadedEntities.Values)
        {
            if (entity is DroppedItem)
            {
                continue;
            }

            if (collisionBoxes.Any(entity.Hitbox.Intersects))
            {
                return true;
            }
        }

        return false;
    }

    public bool IsOutsideBuildHeight(int worldY)
    {
        return worldY < 0 || worldY >= Constants.MAX_BUILD_HEIGHT;
    }

    public bool IsBlockPositionLoaded(Vector3i blockPos)
    {
        return !IsOutsideBuildHeight(blockPos.Y) &&
               _loadedChunks.ContainsKey(GetChunkPosition(blockPos.X, blockPos.Z));
    }

    public BlockState GetBlockAt(Vector3i blockPos)
    {
        Vector2 chunkPos = GetChunkPosition(blockPos.X, blockPos.Z);
        if (!_loadedChunks.TryGetValue(chunkPos, out Chunk? chunk))
        {
            return BlockRegistry.GetState(BlockRegistry.Air);
        }

        return chunk.GetBlockAt(blockPos.ToChunkLocal());
    }

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
