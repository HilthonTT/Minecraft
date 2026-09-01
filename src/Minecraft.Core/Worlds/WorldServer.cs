using Minecraft.Core.Entities;
using Minecraft.Core.Entities.Mobs;
using Minecraft.Core.Entities.Player;
using Minecraft.Core.Games;
using Minecraft.Core.Inventories;
using Minecraft.Core.Network.Packets;
using Minecraft.Core.Network.Session;
using Minecraft.Core.Worlds.Blocks;
using Minecraft.Core.Worlds.Chunks;
using Minecraft.Core.Worlds.Generation;
using Minecraft.Core.Worlds.Storage;
using OpenTK.Mathematics;

namespace Minecraft.Core.Worlds;

public sealed class WorldServer : World
{
    private const int SpawnAreaRadius = 3;

    private const float AutoSaveIntervalSeconds = 60;

    private readonly IdTracker _entityIdTracker = new();
    private readonly MobSpawner _mobSpawner = new();
    private readonly WorldGenerator _worldGenerator;
    private readonly WorldStorage _storage;
    private readonly WorldMetadata _metadata;

    private float _elapsedSecondsSinceAutoSave;

    private const float DropTossSpeed = 2.2F;

    private const float DropTossLift = 12F;

    private const float ThrowSpeed = 20F;

    private const float ThrowLift = 20F;

    private const float ThrowHeightFraction = 0.7F;

    private readonly List<DroppedItem> _itemsToClear = [];

    private readonly Dictionary<Vector3i, ItemStack> _dropsAwaitingRemoval = [];

    public int Seed => _metadata.Seed;

    public GameMode DefaultGameMode
    {
        get => _metadata.GameMode;
        set => _metadata.GameMode = value;
    }

    public WorldServer(Game game, WorldStorage storage, int? seed, GameMode? gameMode) : base(game)
    {
        OnBlockPlacedHandler += OnBlockPlacedServer;
        OnBlockRemovedHandler += OnBlockRemovedServer;
        OnEntityDespawnedHandler += OnEntityDespawnedServer;

        _storage = storage;
        _metadata = storage.LoadOrCreateMetadata(seed, gameMode);
        Environment.CurrentTime = _metadata.CurrentTime;

        _worldGenerator = new WorldGenerator(this, storage, _metadata.Seed);

        _storage.SaveMetadata(_metadata);

        LoadSpawnArea();
    }

    public override void Update(float deltaTimeSeconds)
    {
        base.Update(deltaTimeSeconds);

        _dropsAwaitingRemoval.Clear();

        _elapsedSecondsSinceAutoSave += deltaTimeSeconds;
        if (_elapsedSecondsSinceAutoSave >= AutoSaveIntervalSeconds)
        {
            _elapsedSecondsSinceAutoSave = 0;
            Save();
        }
    }

    protected override void OnTick(float deltaTime)
    {
        _mobSpawner.Tick(this);
        TickDroppedItems();
        TickPlayerRecovery(deltaTime);
    }

    private void TickDroppedItems()
    {
        _itemsToClear.Clear();

        foreach (Entity entity in LoadedEntities.Values)
        {
            if (entity is not DroppedItem item)
            {
                continue;
            }

            if (item.HasExpired)
            {
                _itemsToClear.Add(item);
                continue;
            }

            ServerPlayer? collector = item.FindCollector(this);
            if (collector is null)
            {
                continue;
            }

            SessionOf(collector)?.WritePacket(new ItemPickupPacket(
                item.ID,
                item.Stack.Item!.Id,
                item.Stack.Count,
                item.Stack.Damage));

            _itemsToClear.Add(item);
        }

        foreach (DroppedItem item in _itemsToClear)
        {
            DespawnEntity(item.ID);
        }
    }

    private void TickPlayerRecovery(float deltaTime)
    {
        foreach (ServerSession session in Game.Server.ConnectedClients)
        {
            if (session.Player is ServerPlayer player && player.TryRegenerate(deltaTime))
            {
                session.WritePacket(new PlayerHealthPacket(player.Health, wasHurt: false));
            }
        }
    }

    private ServerSession? SessionOf(ServerPlayer player)
    {
        foreach (ServerSession session in Game.Server.ConnectedClients)
        {
            if (ReferenceEquals(session.Player, player))
            {
                return session;
            }
        }

        return null;
    }

    public void Save()
    {
        foreach (Chunk chunk in LoadedChunks.Values)
        {
            _storage.QueueChunkSave(chunk);
        }

        _metadata.CurrentTime = Environment.CurrentTime;
        _storage.SaveMetadata(_metadata);
    }

    public void SaveAndFlush()
    {
        Save();
        _storage.Flush();
    }

    protected override void OnChunkUnloadedPostProcess(Chunk chunk)
    {
        _storage.QueueChunkSave(chunk);
        base.OnChunkUnloadedPostProcess(chunk);
    }

    private const int MaxSpawnSearchRadiusInChunks = 40;

    private void LoadSpawnArea()
    {
        (int centerChunkX, int centerChunkZ) = FindDryLandChunkNearOrigin() ?? (0, 0);

        for (int x = -SpawnAreaRadius; x <= SpawnAreaRadius; x++)
        {
            for (int y = -SpawnAreaRadius; y <= SpawnAreaRadius; y++)
            {
                AddPlayerPresenceToChunk(_worldGenerator.ProvideChunkAt(centerChunkX + x, centerChunkZ + y));
            }
        }
    }

    private (int ChunkX, int ChunkZ)? FindDryLandChunkNearOrigin()
    {
        for (int radius = 0; radius <= MaxSpawnSearchRadiusInChunks; radius++)
        {
            for (int chunkX = -radius; chunkX <= radius; chunkX++)
            {
                for (int chunkZ = -radius; chunkZ <= radius; chunkZ++)
                {
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

        return new Vector3(x, _worldGenerator.SeaLevel + 1, z);
    }

    private Vector3? TryFindDryLandSpawnIn(int chunkX, int chunkZ)
    {
        int worldX = (chunkX * 16) + 8;
        int worldZ = (chunkZ * 16) + 8;

        if (_worldGenerator.TerrainSampler.SampleColumn(worldX, worldZ).SurfaceY <= _worldGenerator.SeaLevel)
        {
            return null;
        }

        AddPlayerPresenceToChunk(_worldGenerator.ProvideChunkAt(chunkX, chunkZ));

        for (int y = Constants.MAX_BUILD_HEIGHT - 4; y >= _worldGenerator.SeaLevel; y--)
        {
            if (HasSolidBlockAt(new Vector3i(worldX, y, worldZ)))
            {
                return new Vector3(worldX, y + 1, worldZ);
            }
        }

        return null;
    }

    private bool HasSolidBlockAt(Vector3i blockPos)
    {
        BlockState blockState = GetBlockAt(blockPos);
        return blockState.GetBlock().GetCollisionBox(blockState, blockPos).Length > 0;
    }

    public void HurtMob(Mob mob, int damage, Vector3 from, Entity? attacker = null, float knockbackMultiplier = 1F)
    {
        if (!mob.TryHurt(damage, from, attacker, knockbackMultiplier))
        {
            return;
        }

        var packet = new EntityHurtPacket(mob.ID, died: !mob.IsAlive);

        foreach (ServerSession session in Game.Server.ConnectedClients)
        {
            if (session.IsChunkVisible(GetChunkPosition(mob.Position.X, mob.Position.Z)))
            {
                session.WritePacket(packet);
            }
        }

        if (!mob.IsAlive)
        {
            DespawnEntity(mob.ID);
        }
    }

    public void DropWhenRemoved(Vector3i blockPos, ItemStack stack)
    {
        if (!stack.IsEmpty)
        {
            _dropsAwaitingRemoval[blockPos] = stack;
        }
    }

    public void ThrowDroppedItem(ServerPlayer thrower, ItemStack stack)
    {
        if (stack.IsEmpty)
        {
            return;
        }

        var from = new Vector3(
            thrower.Position.X + (thrower.Width / 2F) - (DroppedItem.BodySize / 2F),
            thrower.Position.Y + (thrower.Height * ThrowHeightFraction),
            thrower.Position.Z + (thrower.Length / 2F) - (DroppedItem.BodySize / 2F));

        var forward = new Vector3(MathF.Sin(thrower.Yaw), 0F, MathF.Cos(thrower.Yaw));

        var item = new DroppedItem(
            GenerateEntityId(),
            this,
            from,
            stack,
            DroppedItem.ThrownPickupDelaySeconds)
        {
            Velocity = new Vector3(forward.X * ThrowSpeed, ThrowLift, forward.Z * ThrowSpeed),
        };

        SpawnEntity(item);
    }

    public void SpawnDroppedItem(Vector3i blockPos, ItemStack stack)
    {
        if (stack.IsEmpty)
        {
            return;
        }

        var position = new Vector3(
            blockPos.X + 0.5F - (DroppedItem.BodySize / 2F),
            blockPos.Y + 0.5F - (DroppedItem.BodySize / 2F),
            blockPos.Z + 0.5F - (DroppedItem.BodySize / 2F));

        var item = new DroppedItem(GenerateEntityId(), this, position, stack)
        {
            Velocity = new Vector3(
                ((float)Random.Shared.NextDouble() - 0.5F) * DropTossSpeed,
                DropTossLift,
                ((float)Random.Shared.NextDouble() - 0.5F) * DropTossSpeed),
        };

        SpawnEntity(item);
    }

    public void HurtPlayer(ServerPlayer player, int damage)
    {
        if (!player.TryHurt(damage))
        {
            return;
        }

        ServerSession? session = SessionOf(player);
        session?.WritePacket(new PlayerHealthPacket(player.Health, wasHurt: true));

        if (player.IsAlive)
        {
            return;
        }

        Vector3 spawn = GenerateAndGetValidSpawn();
        player.Respawn(spawn);

        session?.WritePacket(new PlayerRespawnPacket(spawn));
        session?.WritePacket(new PlayerHealthPacket(player.Health, wasHurt: false));
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
        if (_dropsAwaitingRemoval.Remove(blockPos, out ItemStack dropped))
        {
            SpawnDroppedItem(blockPos, dropped);
        }

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
