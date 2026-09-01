using Minecraft.Core.Utilities.Vectors;
using Minecraft.Core.Worlds;
using Minecraft.Core.Worlds.Blocks;
using Minecraft.Core.Worlds.Chunks;
using OpenTK.Mathematics;

namespace Minecraft.Core.Entities.Mobs;

public sealed class MobSpawner
{
    private const int TicksBetweenRounds = 20;

    private const int RoundsBetweenAnimalRounds = 3;

    private const int HostileAttemptsPerRound = 20;
    private const int AnimalAttemptsPerRound = 6;

    private const int MaxAnimalsPerPlayer = 15;
    private const int MaxHostilesPerPlayer = 14;

    private const float MobCapDistance = 56F;

    private const int SunlightRoundsBeforeBurningUp = 3;

    private const float MinDistanceFromPlayer = 14F;

    private const float MinHostileDistanceFromPlayer = 28F;

    private const float MaxDistanceFromPlayer = 44F;

    private const float HostileDespawnDistance = 72F;

    private const float AnimalDespawnDistance = 112F;

    private const int RequiredHeadroomBlocks = 2;

    private const int SurfaceSearchDepth = 12;

    private const int UndergroundSearchDepth = 10;

    private const int PackSpreadBlocks = 4;

    private const int LightSourceExclusionRadius = 12;

    private const int SunlightSpreadDistance = 7;

    private const int SunlightColumnSearchDepth = 32;

    private readonly record struct SpawnEntry(EntityType Type, int Weight, int MinPackSize, int MaxPackSize);

    private static readonly SpawnEntry[] AnimalSpawns =
    [
        new(EntityType.Sheep, Weight: 12, MinPackSize: 2, MaxPackSize: 4),
        new(EntityType.Pig, Weight: 10, MinPackSize: 2, MaxPackSize: 4),
        new(EntityType.Cow, Weight: 8, MinPackSize: 2, MaxPackSize: 4),
    ];

    private static readonly SpawnEntry[] HostileSpawns =
    [
        new(EntityType.Zombie, Weight: 1, MinPackSize: 1, MaxPackSize: 4),
    ];

    private readonly List<Player.Player> _players = [];
    private readonly List<Chunk> _spawnableChunks = [];
    private readonly List<Mob> _toDespawn = [];

    private Dictionary<int, int> _sunlightRounds = [];
    private Dictionary<int, int> _nextSunlightRounds = [];

    private int _ticksUntilNextRound;
    private int _roundsUntilNextAnimalRound;

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

        DespawnUnwantedMobs(world);

        CollectSpawnableChunks(world);
        if (_spawnableChunks.Count == 0)
        {
            return;
        }

        TrySpawnRound(world, HostileSpawns, hostile: true, HostileAttemptsPerRound);

        if (_roundsUntilNextAnimalRound > 0)
        {
            _roundsUntilNextAnimalRound--;
            return;
        }

        _roundsUntilNextAnimalRound = RoundsBetweenAnimalRounds;
        TrySpawnRound(world, AnimalSpawns, hostile: false, AnimalAttemptsPerRound);
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

    private void DespawnUnwantedMobs(WorldServer world)
    {
        _toDespawn.Clear();
        _nextSunlightRounds.Clear();

        bool isSunUp = !world.Environment.IsNight;

        foreach (Entity entity in world.LoadedEntities.Values)
        {
            if (entity is not Mob mob)
            {
                continue;
            }

            if (ShouldDespawn(world, mob, isSunUp))
            {
                _toDespawn.Add(mob);
            }
        }

        (_sunlightRounds, _nextSunlightRounds) = (_nextSunlightRounds, _sunlightRounds);

        foreach (Mob mob in _toDespawn)
        {
            world.DespawnEntity(mob.ID);
        }
    }

    private bool ShouldDespawn(World world, Mob mob, bool isSunUp)
    {
        if (!mob.IsHostile)
        {
            return !IsAnyPlayerWithin(mob.Position, AnimalDespawnDistance);
        }

        if (!IsAnyPlayerWithin(mob.Position, HostileDespawnDistance))
        {
            return true;
        }

        return IsBurningUpInSunlight(world, mob, isSunUp);
    }

    private bool IsBurningUpInSunlight(World world, Mob mob, bool isSunUp)
    {
        if (!isSunUp || !IsReachedBySunlight(world, mob.Position.ToBlockPos()))
        {
            return false;
        }

        int roundsInSunlight = _sunlightRounds.GetValueOrDefault(mob.ID) + 1;
        if (roundsInSunlight >= SunlightRoundsBeforeBurningUp)
        {
            return true;
        }

        _nextSunlightRounds[mob.ID] = roundsInSunlight;
        return false;
    }

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

    private void TrySpawnRound(WorldServer world, SpawnEntry[] spawns, bool hostile, int attempts)
    {
        int mobCount = CountMobsNearPlayers(world, hostile);
        int mobLimit = (hostile ? MaxHostilesPerPlayer : MaxAnimalsPerPlayer) * _players.Count;

        for (int attempt = 0; attempt < attempts && mobCount < mobLimit; attempt++)
        {
            mobCount += TrySpawnPack(world, spawns, hostile);
        }
    }

    private int CountMobsNearPlayers(WorldServer world, bool hostile)
    {
        int count = 0;

        foreach (Entity entity in world.LoadedEntities.Values)
        {
            if (entity is Mob mob && mob.IsHostile == hostile && IsAnyPlayerWithin(mob.Position, MobCapDistance))
            {
                count++;
            }
        }

        return count;
    }

    private int TrySpawnPack(WorldServer world, SpawnEntry[] spawns, bool hostile)
    {
        Chunk chunk = _spawnableChunks[Random.Shared.Next(_spawnableChunks.Count)];
        int anchorX = chunk.GridX * 16 + Random.Shared.Next(16);
        int anchorZ = chunk.GridZ * 16 + Random.Shared.Next(16);

        int columnTop = GetColumnTop(chunk, anchorX, anchorZ);

        bool aimUnderground = hostile && (!world.Environment.IsDarkOutside || Random.Shared.Next(2) == 0);

        (int startY, int searchDepth) = aimUnderground
            ? (Random.Shared.Next(2, Math.Max(3, columnTop + 1)), UndergroundSearchDepth)
            : (columnTop + 1, SurfaceSearchDepth);

        if (!IsUsableSpawnColumn(world, anchorX, anchorZ, startY, searchDepth, hostile, out int anchorFeetY))
        {
            return 0;
        }

        SpawnEntry entry = PickSpawnEntry(spawns);
        int packSize = Random.Shared.Next(entry.MinPackSize, entry.MaxPackSize + 1);

        int spawned = Spawn(world, entry.Type, anchorX, anchorFeetY, anchorZ) ? 1 : 0;

        for (int member = 1; member < packSize; member++)
        {
            int memberX = anchorX + Random.Shared.Next(-PackSpreadBlocks, PackSpreadBlocks + 1);
            int memberZ = anchorZ + Random.Shared.Next(-PackSpreadBlocks, PackSpreadBlocks + 1);

            if (IsUsableSpawnColumn(
                    world,
                    memberX,
                    memberZ,
                    anchorFeetY + RequiredHeadroomBlocks,
                    RequiredHeadroomBlocks + PackSpreadBlocks,
                    hostile,
                    out int memberFeetY)
                && Spawn(world, entry.Type, memberX, memberFeetY, memberZ))
            {
                spawned++;
            }
        }

        return spawned;
    }

    private static SpawnEntry PickSpawnEntry(SpawnEntry[] spawns)
    {
        int totalWeight = 0;
        foreach (SpawnEntry entry in spawns)
        {
            totalWeight += entry.Weight;
        }

        int roll = Random.Shared.Next(totalWeight);

        foreach (SpawnEntry entry in spawns)
        {
            roll -= entry.Weight;
            if (roll < 0)
            {
                return entry;
            }
        }

        return spawns[^1];
    }

    private bool IsUsableSpawnColumn(
        World world,
        int worldX,
        int worldZ,
        int startY,
        int searchDepth,
        bool hostile,
        out int feetY)
    {
        if (!TryFindFloor(world, worldX, worldZ, startY, searchDepth, out feetY))
        {
            return false;
        }

        var feet = new Vector3i(worldX, feetY, worldZ);

        if (!IsGroundMobsWillStandOn(world, feet.Down(), hostile))
        {
            return false;
        }

        if (hostile && !IsDarkEnoughForHostiles(world, feet))
        {
            return false;
        }

        return IsAtUsableDistanceFromPlayers(new Vector3(worldX + 0.5F, feetY, worldZ + 0.5F), hostile);
    }

    private static bool Spawn(WorldServer world, EntityType entityType, int worldX, int feetY, int worldZ)
    {
        var feet = new Vector3(worldX + 0.5F, feetY, worldZ + 0.5F);

        Mob? mob = MobFactory.Create(entityType, world.GenerateEntityId(), world, feet);
        if (mob is null)
        {
            return false;
        }

        mob.Yaw = Random.Shared.NextSingle() * MathF.Tau;
        world.SpawnEntity(mob);
        return true;
    }

    private static int GetColumnTop(Chunk chunk, int worldX, int worldZ)
    {
        return Math.Min(
            chunk.TopMostBlocks[worldX & 15, worldZ & 15],
            Constants.MAX_BUILD_HEIGHT - RequiredHeadroomBlocks - 1);
    }

    private static bool TryFindFloor(World world, int worldX, int worldZ, int startY, int searchDepth, out int feetY)
    {
        feetY = 0;

        int highest = Math.Min(startY, Constants.MAX_BUILD_HEIGHT - RequiredHeadroomBlocks - 1);

        int lowest = Math.Max(1, highest - searchDepth);

        for (int y = highest; y >= lowest; y--)
        {
            if (!IsPassable(world, new Vector3i(worldX, y, worldZ)) ||
                IsPassable(world, new Vector3i(worldX, y - 1, worldZ)))
            {
                continue;
            }

            feetY = y;
            return HasHeadroom(world, worldX, y, worldZ);
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

    private static bool IsPassable(World world, Vector3i blockPos)
    {
        BlockState state = world.GetBlockAt(blockPos);
        Block block = state.GetBlock();

        if (block.IsLiquid)
        {
            return false;
        }

        return block == BlockRegistry.Air || block.GetCollisionBox(state, blockPos).Length == 0;
    }

    private static bool IsGroundMobsWillStandOn(World world, Vector3i groundPos, bool hostile)
    {
        Block block = world.GetBlockAt(groundPos).GetBlock();

        if (!hostile)
        {
            return block == BlockRegistry.Grass || block == BlockRegistry.SnowyGrass;
        }

        return block.IsOpaque;
    }

    private static bool IsDarkEnoughForHostiles(World world, Vector3i feet)
    {
        if (!world.Environment.IsDarkOutside && IsReachedBySunlight(world, feet))
        {
            return false;
        }

        return !IsNearLightSource(world, feet);
    }

    private static bool IsReachedBySunlight(World world, Vector3i blockPos)
    {
        if (IsSunlitColumnWithinReach(world, blockPos, offsetX: 0, offsetZ: 0))
        {
            return true;
        }

        for (int offsetX = -SunlightSpreadDistance; offsetX <= SunlightSpreadDistance; offsetX++)
        {
            int remaining = SunlightSpreadDistance - Math.Abs(offsetX);

            for (int offsetZ = -remaining; offsetZ <= remaining; offsetZ++)
            {
                if (IsSunlitColumnWithinReach(world, blockPos, offsetX, offsetZ))
                {
                    return true;
                }
            }
        }

        return false;
    }

    private static bool IsSunlitColumnWithinReach(World world, Vector3i blockPos, int offsetX, int offsetZ)
    {
        int worldX = blockPos.X + offsetX;
        int worldZ = blockPos.Z + offsetZ;

        Vector2 chunkPos = World.GetChunkPosition(worldX, worldZ);

        if (!world.LoadedChunks.TryGetValue(chunkPos, out Chunk? chunk))
        {
            return false;
        }

        int sunlitFloorY = GetSunlitFloorOfColumn(chunk, worldX & 15, worldZ & 15);

        int distance = Math.Abs(offsetX) + Math.Abs(offsetZ) + Math.Max(0, sunlitFloorY - blockPos.Y);
        return distance <= SunlightSpreadDistance;
    }

    private static int GetSunlitFloorOfColumn(Chunk chunk, int localX, int localZ)
    {
        int highest = Math.Min(chunk.TopMostBlocks[localX, localZ], Constants.MAX_BUILD_HEIGHT - 1);
        int lowest = Math.Max(0, highest - SunlightColumnSearchDepth);

        for (int y = highest; y >= lowest; y--)
        {
            if (chunk.GetBlockAt(localX, y, localZ).GetBlock().IsOpaque)
            {
                return y + 1;
            }
        }

        return lowest;
    }

    private static bool IsNearLightSource(World world, Vector3i blockPos)
    {
        Vector2 centre = World.GetChunkPosition(blockPos.X, blockPos.Z);

        for (int offsetX = -1; offsetX <= 1; offsetX++)
        {
            for (int offsetZ = -1; offsetZ <= 1; offsetZ++)
            {
                var chunkPos = new Vector2(centre.X + offsetX, centre.Y + offsetZ);
                if (!world.LoadedChunks.TryGetValue(chunkPos, out Chunk? chunk))
                {
                    continue;
                }

                foreach (Vector3i lightPos in chunk.LightSourceBlocks.Keys)
                {
                    int dx = lightPos.X - blockPos.X;
                    int dy = lightPos.Y - blockPos.Y;
                    int dz = lightPos.Z - blockPos.Z;

                    if (dx * dx + dy * dy + dz * dz <= LightSourceExclusionRadius * LightSourceExclusionRadius)
                    {
                        return true;
                    }
                }
            }
        }

        return false;
    }

    private bool IsAtUsableDistanceFromPlayers(Vector3 position, bool hostile)
    {
        float minDistance = hostile ? MinHostileDistanceFromPlayer : MinDistanceFromPlayer;
        bool anyInRange = false;

        foreach (Player.Player player in _players)
        {
            float distanceSquared = (player.Position - position).LengthSquared;

            if (distanceSquared < minDistance * minDistance)
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
