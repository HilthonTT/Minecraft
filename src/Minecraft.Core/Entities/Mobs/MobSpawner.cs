using Minecraft.Core.Worlds;
using Minecraft.Core.Worlds.Blocks;
using Minecraft.Core.Worlds.Chunks;
using OpenTK.Mathematics;

namespace Minecraft.Core.Entities.Mobs;

/// <summary>
/// Populates the loaded world with mobs, and clears them out again once nobody is near them. Runs on the
/// server only; clients hear about every mob through the entity tracker on their session.
/// <para>
/// Mobs appear on the surface of a column and nowhere else, so caves stay empty however dark they are, and
/// which mob appears comes down to the hour rather than to how lit the spot is. Light maps are built by the
/// renderer on each client and never on the server, so there is no light level here to read.
/// </para>
/// </summary>
public sealed class MobSpawner
{
    /// <summary>Ticks between two rounds of spawning. A tick is a twentieth of a second.</summary>
    private const int TicksBetweenRounds = 20;

    /// <summary>Positions tried per round. Most are rejected, so this is not how many mobs appear.</summary>
    private const int AttemptsPerRound = 12;

    /// <summary>How many mobs may exist for each player online.</summary>
    private const int MaxMobsPerPlayer = 10;

    /// <summary>Mobs never appear closer to a player than this, so nothing is seen popping into existence.</summary>
    private const float MinDistanceFromPlayer = 14F;

    /// <summary>Nor further off than this, which keeps them inside the area somebody has loaded.</summary>
    private const float MaxDistanceFromPlayer = 44F;

    /// <summary>A mob further than this from every player is removed again.</summary>
    private const float DespawnDistance = 72F;

    /// <summary>Blocks of clear space a mob needs above the ground to fit.</summary>
    private const int RequiredHeadroomBlocks = 2;

    /// <summary>How far down from the recorded top of a column the ground is searched for.</summary>
    private const int SurfaceSearchDepth = 12;

    private readonly List<Player.Player> _players = [];
    private readonly List<Chunk> _spawnableChunks = [];
    private readonly List<Mob> _toDespawn = [];

    private int _ticksUntilNextRound;

    public void Tick(WorldServer world)
    {
        if (_ticksUntilNextRound > 0)
        {
            _ticksUntilNextRound--;
            return;
        }

        _ticksUntilNextRound = TicksBetweenRounds;

        CollectPlayers(world);
        if (_players.Count == 0)
        {
            return;
        }

        DespawnDistantMobs(world);
        TrySpawnRound(world);
    }

    private void CollectPlayers(WorldServer world)
    {
        _players.Clear();

        foreach (Entity entity in world.LoadedEntities.Values)
        {
            if (entity is Player.Player player)
            {
                _players.Add(player);
            }
        }
    }

    private void DespawnDistantMobs(WorldServer world)
    {
        _toDespawn.Clear();

        foreach (Entity entity in world.LoadedEntities.Values)
        {
            if (entity is Mob mob && !IsAnyPlayerWithin(mob.Position, DespawnDistance))
            {
                _toDespawn.Add(mob);
            }
        }

        // Collected first, because despawning removes the entity from the collection being walked.
        foreach (Mob mob in _toDespawn)
        {
            world.DespawnEntity(mob.ID);
        }
    }

    private void TrySpawnRound(WorldServer world)
    {
        int mobCount = CountMobs(world);
        int mobLimit = MaxMobsPerPlayer * _players.Count;

        if (mobCount >= mobLimit)
        {
            return;
        }

        CollectSpawnableChunks(world);
        if (_spawnableChunks.Count == 0)
        {
            return;
        }

        for (int attempt = 0; attempt < AttemptsPerRound && mobCount < mobLimit; attempt++)
        {
            if (TrySpawnOne(world))
            {
                mobCount++;
            }
        }
    }

    private static int CountMobs(WorldServer world)
    {
        int count = 0;

        foreach (Entity entity in world.LoadedEntities.Values)
        {
            if (entity is Mob)
            {
                count++;
            }
        }

        return count;
    }

    /// <summary>
    /// The loaded chunks close enough to a player to be worth trying. Picking from these rather than from
    /// every loaded chunk keeps the attempts where they have a chance of passing the distance check.
    /// </summary>
    private void CollectSpawnableChunks(WorldServer world)
    {
        _spawnableChunks.Clear();

        foreach (Chunk chunk in world.LoadedChunks.Values)
        {
            var chunkCentre = new Vector3(chunk.GridX * 16 + 8, 0, chunk.GridZ * 16 + 8);

            foreach (Player.Player player in _players)
            {
                float dx = player.Position.X - chunkCentre.X;
                float dz = player.Position.Z - chunkCentre.Z;

                if (dx * dx + dz * dz <= MaxDistanceFromPlayer * MaxDistanceFromPlayer)
                {
                    _spawnableChunks.Add(chunk);
                    break;
                }
            }
        }
    }

    private bool TrySpawnOne(WorldServer world)
    {
        Chunk chunk = _spawnableChunks[Random.Shared.Next(_spawnableChunks.Count)];
        int localX = Random.Shared.Next(16);
        int localZ = Random.Shared.Next(16);

        if (!TryFindSpawnHeight(world, chunk, localX, localZ, out int feetY))
        {
            return false;
        }

        // Offset to the middle of the block, so the mob does not start half inside the neighbouring column.
        var feet = new Vector3(chunk.GridX * 16 + localX + 0.5F, feetY, chunk.GridZ * 16 + localZ + 0.5F);

        if (!IsAtUsableDistanceFromPlayers(feet))
        {
            return false;
        }

        // Everything is checked before the mob is built, because building one takes an entity id that would
        // otherwise have to be handed back.
        Mob mob = world.Environment.IsNight
            ? new Zombie(world.GenerateEntityId(), world, feet)
            : new Sheep(world.GenerateEntityId(), world, feet);

        world.SpawnEntity(mob);
        return true;
    }

    /// <summary>
    /// Finds where a mob's feet would rest in the given column, searching down from the top of it. Fails
    /// when there is no ground within reach or nothing standing on it would fit. Searching downwards from
    /// the top is what confines mobs to the surface: the first ground it meets is the highest there is.
    /// </summary>
    private static bool TryFindSpawnHeight(World world, Chunk chunk, int localX, int localZ, out int feetY)
    {
        feetY = 0;

        int worldX = chunk.GridX * 16 + localX;
        int worldZ = chunk.GridZ * 16 + localZ;

        // The recorded column top only ever grows as blocks are added, so it is an upper bound on where the
        // surface is rather than the surface itself, and the ground is searched for downwards from it.
        int highest = Math.Min(
            chunk.TopMostBlocks[localX, localZ],
            Constants.MAX_BUILD_HEIGHT - RequiredHeadroomBlocks - 1);

        for (int y = highest; y > highest - SurfaceSearchDepth && y > 0; y--)
        {
            if (IsPassable(world, new Vector3i(worldX, y, worldZ)))
            {
                continue;
            }

            feetY = y + 1;
            return HasHeadroom(world, worldX, feetY, worldZ);
        }

        return false;
    }

    private static bool HasHeadroom(World world, int worldX, int feetY, int worldZ)
    {
        for (int offset = 0; offset < RequiredHeadroomBlocks; offset++)
        {
            if (!IsPassable(world, new Vector3i(worldX, feetY + offset, worldZ)))
            {
                return false;
            }
        }

        return true;
    }

    /// <summary>
    /// Whether a mob could stand where this block is. Flowers and grass have no collision box, so they are
    /// passable and a field of them is still somewhere a mob can appear.
    /// </summary>
    private static bool IsPassable(World world, Vector3i blockPos)
    {
        BlockState state = world.GetBlockAt(blockPos);
        Block block = state.GetBlock();

        return block == BlockRegistry.Air || block.GetCollisionBox(state, blockPos).Length == 0;
    }

    /// <summary>
    /// Whether the spot is far enough from everybody to appear unseen, while still being near enough to
    /// somebody to be worth having.
    /// </summary>
    private bool IsAtUsableDistanceFromPlayers(Vector3 position)
    {
        bool anyInRange = false;

        foreach (Player.Player player in _players)
        {
            float distanceSquared = (player.Position - position).LengthSquared;

            if (distanceSquared < MinDistanceFromPlayer * MinDistanceFromPlayer)
            {
                return false;
            }

            if (distanceSquared <= MaxDistanceFromPlayer * MaxDistanceFromPlayer)
            {
                anyInRange = true;
            }
        }

        return anyInRange;
    }

    private bool IsAnyPlayerWithin(Vector3 position, float distance)
    {
        foreach (Player.Player player in _players)
        {
            if ((player.Position - position).LengthSquared <= distance * distance)
            {
                return true;
            }
        }

        return false;
    }

}
