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

    /// <summary>How hard a drop is tossed out of the block it came from, in blocks per second.</summary>
    private const float DropTossSpeed = 2.2F;

    /// <summary>
    /// The lift on that toss, so a drop hops out of its cell rather than sliding along the floor. A speed
    /// rather than the force a jump is expressed as, since it is written straight into the velocity: against
    /// this world's gravity it comes out as a hop of about an eighth of a block, which is what a block
    /// coming loose should look like rather than something being thrown into the air.
    /// </summary>
    private const float DropTossLift = 12F;

    /// <summary>
    /// How hard a player throws something down, in blocks per second. Movement here is damped hard enough
    /// that a body keeps about a tenth of a second of whatever speed it is given, so this comes out as a
    /// throw of roughly two blocks — which is what it has to be. Anything shorter lands inside the reach a
    /// stack is picked up from, and the key would only hand it back the moment the throw wore off.
    /// </summary>
    private const float ThrowSpeed = 20F;

    /// <summary>
    /// The lift on a throw, which arcs it out rather than skidding it along the floor. Against this world's
    /// gravity it tops out about half a block up, so a stack goes over a fence rather than into it.
    /// </summary>
    private const float ThrowLift = 20F;

    /// <summary>
    /// How far up a player's body a throw leaves from, as a share of their height. About chest high, so a
    /// stack arcs out of the hands rather than off the boots.
    /// </summary>
    private const float ThrowHeightFraction = 0.7F;

    /// <summary>Reused when sweeping up the items that have been collected or have lain there too long.</summary>
    private readonly List<DroppedItem> _itemsToClear = [];

    /// <summary>
    /// What each cell is to leave behind when the block in it goes, put here by whatever asked for the
    /// removal and taken out again by the removal itself. See <see cref="DropWhenRemoved"/>.
    /// </summary>
    private readonly Dictionary<Vector3i, ItemStack> _dropsAwaitingRemoval = [];

    /// <summary>
    /// The seed the terrain came out of. Read back rather than taken from what was asked for, since a world
    /// that already existed keeps its own and one left to choose picked its own.
    /// </summary>
    public int Seed => _metadata.Seed;

    /// <summary>
    /// Which mode this world is played in, which is what anyone joining is put into. Fixed when the world is
    /// created, the same way its seed is, and moved afterwards only by <c>/gamemode</c>.
    /// </summary>
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

        // Written straight away so the seed is on disk even if the process never shuts down cleanly.
        _storage.SaveMetadata(_metadata);

        LoadSpawnArea();
    }

    public override void Update(float deltaTimeSeconds)
    {
        base.Update(deltaTimeSeconds);

        // Every removal queued for this frame has been carried out by the time the base call returns, so
        // anything still waiting here is for a removal that was refused — an unloaded chunk, or a cell that
        // turned out to be air already. Forgotten rather than kept, or it would pay out the next time
        // something entirely different was broken in that same cell.
        _dropsAwaitingRemoval.Clear();

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
        TickDroppedItems();
        TickPlayerRecovery(deltaTime);
    }

    /// <summary>
    /// Hands every item lying on the ground to whoever is standing over it, and clears away whatever nobody
    /// came back for. Walked once a tick rather than watched per player, since there are far fewer items in
    /// a world than there are frames in a second.
    /// </summary>
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

            // What the item is worth goes to the one player who picked it up, since only their client holds
            // the inventory it lands in. Everyone else simply sees it stop being there.
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

    /// <summary>Mends the players who have been left alone long enough, and tells them so.</summary>
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

    /// <summary>The connection a given player is at the other end of, or null once they have left.</summary>
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

    /// <summary>
    /// Hurts a mob, tells everyone who can see it, and takes it out of the world if that was the last blow
    /// it had in it. The one road by which anything in the world loses health, so a punch and a blast are
    /// reported to a client in the same words and neither has to know how the other goes about it.
    /// <para>
    /// Nothing happens for a blow that did not land — one inside the window a harder blow already opened —
    /// so nothing is broadcast for it either, and a client is never told about a hit it should not show.
    /// </para>
    /// </summary>
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

    /// <summary>
    /// Says what the block in a cell is to leave behind when it is taken away.
    /// <para>
    /// Asked for rather than done, because a removal is queued and does not happen until the end of the
    /// world update it was asked for in — and the caller is the packet handler, which runs after that, so a
    /// drop thrown out there would exist for a whole frame inside a block that is still solid. What happens
    /// to a body inside a block is that the world lifts it out onto the top of it, four cells if it has to,
    /// so breaking the bottom log of a tree would shoot the drop up the trunk and leave it on the canopy.
    /// Hanging it off the removal instead means the cell is already air by the time anything is put in it.
    /// </para>
    /// </summary>
    public void DropWhenRemoved(Vector3i blockPos, ItemStack stack)
    {
        if (!stack.IsEmpty)
        {
            _dropsAwaitingRemoval[blockPos] = stack;
        }
    }

    /// <summary>
    /// Throws a stack out in front of a player, which is what the drop key does.
    /// <para>
    /// Along the way they are facing and nothing more: the server is told a yaw every tenth of a second and
    /// is not told a pitch at all, so a throw goes out level rather than wherever the head happens to be
    /// pointing. That is the better of the two anyway — a throw that followed the eye would bury a stack in
    /// the floor whenever somebody happened to be looking down at the time.
    /// </para>
    /// <para>
    /// It leaves from inside the thrower rather than a step in front of them, because a step in front can be
    /// inside a wall. Starting where the player is standing is the one place known to be clear, and the
    /// throw carries it out of there under the ordinary collision every entity gets.
    /// </para>
    /// </summary>
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

        // The same basis a player walks along, so a throw goes where they are facing rather than a quarter
        // turn off it.
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

    /// <summary>
    /// Throws a stack out onto the ground at the middle of the given cell, tossed a little off centre so
    /// that a run of drops from a seam of ore spreads out instead of stacking into one point.
    /// </summary>
    public void SpawnDroppedItem(Vector3i blockPos, ItemStack stack)
    {
        if (stack.IsEmpty)
        {
            return;
        }

        // Placed at the middle of the cell less half the item's own body, so it starts centred rather than
        // with its corner on the middle: an entity is built out from its minimum corner.
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

    /// <summary>
    /// Hurts a player and tells them, and puts them back at the spawn if that was the last of it. The one
    /// road by which a player loses health, so a zombie's swing and a landing from a great height are
    /// reported in the same words, and neither has to know how the other goes about it.
    /// </summary>
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

        // Nothing is dropped on death, and the inventory survives it. What a full set of tools costs is a
        // seam of ore found and dug out, and losing that to a single fall is a longer walk back than the
        // death is worth being a punishment for.
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
        // The cell is already air by the time this runs, which is the whole point of waiting for it.
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
