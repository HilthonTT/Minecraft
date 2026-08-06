using Minecraft.Core.Entities;
using Minecraft.Core.Entities.Mobs;
using Minecraft.Core.Entities.Player;
using Minecraft.Core.Games;
using Minecraft.Core.Network.Packets;
using Minecraft.Core.Network.Session;
using Minecraft.Core.Worlds.Blocks;
using Minecraft.Core.Worlds.Chunks;
using Minecraft.Core.Worlds.Generation;
using Minecraft.Core.Worlds.Storage;
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

    /// <summary>How often the world writes itself out while running, in seconds.</summary>
    private const float AutoSaveIntervalSeconds = 60;

    private readonly IdTracker _entityIdTracker = new();
    private readonly MobSpawner _mobSpawner = new();
    private readonly WorldGenerator _worldGenerator;
    private readonly WorldStorage _storage;
    private readonly WorldMetadata _metadata;

    private float _elapsedSecondsSinceAutoSave;

    /// <summary>
    /// The seed the terrain came out of. Read back rather than taken from what was asked for, since a world
    /// that already existed keeps its own and one left to choose picked its own.
    /// </summary>
    public int Seed => _metadata.Seed;

    public WorldServer(Game game, WorldStorage storage, int? seed) : base(game)
    {
        OnBlockPlacedHandler += OnBlockPlacedServer;
        OnBlockRemovedHandler += OnBlockRemovedServer;
        OnEntityDespawnedHandler += OnEntityDespawnedServer;

        _storage = storage;
        _metadata = storage.LoadOrCreateMetadata(seed);
        Environment.CurrentTime = _metadata.CurrentTime;

        _worldGenerator = new WorldGenerator(this, storage, _metadata.Seed);

        // Written straight away so the seed is on disk even if the process never shuts down cleanly.
        _storage.SaveMetadata(_metadata);

        LoadSpawnArea();
    }

    public override void Update(float deltaTimeSeconds)
    {
        base.Update(deltaTimeSeconds);

        _elapsedSecondsSinceAutoSave += deltaTimeSeconds;
        if (_elapsedSecondsSinceAutoSave >= AutoSaveIntervalSeconds)
        {
            _elapsedSecondsSinceAutoSave = 0;
            Save();
        }
    }

    /// <summary>
    /// Mobs live only as long as the server is running: nothing writes them to disk, so a world that is
    /// reloaded is repopulated from scratch.
    /// </summary>
    protected override void OnTick(float deltaTime)
    {
        _mobSpawner.Tick(this);
    }

    /// <summary>
    /// Writes the world out. Only chunks that were modified since they were generated or last saved carry
    /// any cost, so this is cheap to call on a timer.
    /// </summary>
    public void Save()
    {
        foreach (Chunk chunk in LoadedChunks.Values)
        {
            _storage.QueueChunkSave(chunk);
        }

        _metadata.CurrentTime = Environment.CurrentTime;
        _storage.SaveMetadata(_metadata);
    }

    /// <summary>Saves everything and waits for it to reach disk. Called when the server shuts down.</summary>
    public void SaveAndFlush()
    {
        Save();
        _storage.Flush();
    }

    /// <summary>
    /// A chunk about to be recycled is the last chance to persist it, so it is written before the pool can
    /// hand the instance out for another position.
    /// </summary>
    protected override void OnChunkUnloadedPostProcess(Chunk chunk)
    {
        _storage.QueueChunkSave(chunk);
        base.OnChunkUnloadedPostProcess(chunk);
    }

    /// <summary>
    /// How far the search for dry land wanders before giving up and laying a platform. Generous enough to
    /// cross an ocean of the size the generator makes, and bounded so a world that somehow had no land at
    /// all would still open.
    /// </summary>
    private const int MaxSpawnSearchRadiusInChunks = 40;

    private void LoadSpawnArea()
    {
        // Centred on where a player will actually be put rather than on the origin, which the search below
        // walks away from whenever the origin turns out to be at sea. Preloading around the origin instead
        // would build a few hundred chunks of open water that nobody is ever standing in.
        (int centerChunkX, int centerChunkZ) = FindDryLandChunkNearOrigin() ?? (0, 0);

        for (int x = -SpawnAreaRadius; x <= SpawnAreaRadius; x++)
        {
            for (int y = -SpawnAreaRadius; y <= SpawnAreaRadius; y++)
            {
                AddPlayerPresenceToChunk(_worldGenerator.ProvideChunkAt(centerChunkX + x, centerChunkZ + y));
            }
        }
    }

    /// <summary>
    /// The nearest chunk to the origin whose middle column stands above the waterline, or null when there is
    /// none within reach. Answered from the generator's terrain sampler, so no chunk has to exist for it.
    /// </summary>
    private (int ChunkX, int ChunkZ)? FindDryLandChunkNearOrigin()
    {
        for (int radius = 0; radius <= MaxSpawnSearchRadiusInChunks; radius++)
        {
            for (int chunkX = -radius; chunkX <= radius; chunkX++)
            {
                for (int chunkZ = -radius; chunkZ <= radius; chunkZ++)
                {
                    // Only the edge of each ring, since everything inside it was covered by a smaller one.
                    if (radius != 0 && Math.Abs(chunkX) != radius && Math.Abs(chunkZ) != radius)
                    {
                        continue;
                    }

                    int worldX = (chunkX * 16) + 8;
                    int worldZ = (chunkZ * 16) + 8;

                    if (_worldGenerator.TerrainSampler.SampleColumn(worldX, worldZ).SurfaceY > _worldGenerator.SeaLevel)
                    {
                        return (chunkX, chunkZ);
                    }
                }
            }
        }

        return null;
    }

    /// <summary>
    /// Creates a suitable spawn position. Destroys blocks if necessary.
    /// <para>
    /// The origin of a world is as likely to fall in an ocean as anywhere else, so somewhere dry is looked
    /// for rather than assumed. The search walks outwards a chunk at a time and asks the generator how high
    /// the ground is, which it can answer for terrain that has not been built yet, so only the one chunk
    /// actually settled on ever has to be generated.
    /// </para>
    /// </summary>
    public Vector3 GenerateAndGetValidSpawn()
    {
        if (FindDryLandChunkNearOrigin() is (int chunkX, int chunkZ))
        {
            Vector3? spawn = TryFindDryLandSpawnIn(chunkX, chunkZ);
            if (spawn is not null)
            {
                return spawn.Value;
            }
        }

        // Nothing dry within reach, so a platform is laid at the origin to stand on instead.
        const int x = 8;
        const int z = 8;
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

        // On top of the platform that was just laid, not inside it.
        return new Vector3(x, _worldGenerator.SeaLevel + 1, z);
    }

    /// <summary>
    /// A standing position on the surface of the middle column of a chunk, or null when that column is under
    /// water. The chunk is only generated once its ground is known to be dry.
    /// </summary>
    private Vector3? TryFindDryLandSpawnIn(int chunkX, int chunkZ)
    {
        int worldX = (chunkX * 16) + 8;
        int worldZ = (chunkZ * 16) + 8;

        if (_worldGenerator.TerrainSampler.SampleColumn(worldX, worldZ).SurfaceY <= _worldGenerator.SeaLevel)
        {
            return null;
        }

        AddPlayerPresenceToChunk(_worldGenerator.ProvideChunkAt(chunkX, chunkZ));

        // Searched downwards from the sky rather than upwards from sea level. The first solid block met on
        // the way down is the surface, and everything above it is open air, so the player always lands on
        // top of the world. Coming up from below instead stops at the first gap tall enough to stand in,
        // which underground is a cave.
        for (int y = Constants.MAX_BUILD_HEIGHT - 4; y >= _worldGenerator.SeaLevel; y--)
        {
            if (HasSolidBlockAt(new Vector3i(worldX, y, worldZ)))
            {
                return new Vector3(worldX, y + 1, worldZ);
            }
        }

        return null;
    }

    /// <summary>Whether the block at the given position is one that can be stood on.</summary>
    private bool HasSolidBlockAt(Vector3i blockPos)
    {
        BlockState blockState = GetBlockAt(blockPos);
        return blockState.GetBlock().GetCollisionBox(blockState, blockPos).Length > 0;
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