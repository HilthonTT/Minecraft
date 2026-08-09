using Minecraft.Core.Utilities.Vectors;
using Minecraft.Core.Worlds;
using Minecraft.Core.Worlds.Blocks;
using Minecraft.Core.Worlds.Chunks;
using OpenTK.Mathematics;

namespace Minecraft.Core.Entities.Mobs;

/// <summary>
/// Populates the loaded world with mobs, and clears them out again once nobody is near them. Runs on the
/// server only; clients hear about every mob through the entity tracker on their session.
/// <para>
/// Mobs go in as packs rather than one at a time, so what a player comes across is a herd in a field or a
/// knot of zombies in a tunnel, and not a scattering of lone animals. Where a pack may go comes down to what
/// it would be standing on and to whether the spot is dark: animals want grass under them, which keeps them
/// off the sand and the bare rock without the spawner needing to know one biome from another, and hostile
/// mobs want darkness, which they find under the night sky and underground at any hour.
/// </para>
/// <para>
/// There is no light map to read here. Light maps are built by the renderer on each client and never on the
/// server, so where the daylight falls is worked out instead from the heightmap every chunk already keeps:
/// see <see cref="IsReachedBySunlight"/>, which walks the sun down the open columns around a spot the same
/// way the renderer's flood fill would. Torchlight is not modelled at all, only kept away from; see
/// <see cref="IsNearLightSource"/>.
/// </para>
/// </summary>
public sealed class MobSpawner
{
    /// <summary>Ticks between two rounds of spawning. A tick is a twentieth of a second.</summary>
    private const int TicksBetweenRounds = 20;

    /// <summary>
    /// Rounds between two goes at spawning animals. Hostile mobs are tried every round, animals far more
    /// rarely: a herd, once it is there, stays put, so pushing more in every second would only fill the
    /// countryside with livestock. Short enough that a player walking into fresh country does not outrun the
    /// herds appearing in it.
    /// </summary>
    private const int RoundsBetweenAnimalRounds = 3;

    /// <summary>Packs tried per round. Most are rejected, so this is not how many packs appear.</summary>
    private const int HostileAttemptsPerRound = 20;
    private const int AnimalAttemptsPerRound = 6;

    /// <summary>
    /// How many mobs of each kind may stand near a player, for each player online. Counted apart rather than
    /// against one shared total; see <see cref="Mob.IsHostile"/> for why sharing one starves the animals out.
    /// <para>
    /// The allowance for animals has to hold several packs rather than one or two. Animals are not cleared
    /// while a player is about, so the first few packs to go in are the ones that stay, and a cap that only
    /// fits a couple of them leaves whole worlds with no sheep in them purely by the luck of the draw.
    /// </para>
    /// </summary>
    private const int MaxAnimalsPerPlayer = 15;
    private const int MaxHostilesPerPlayer = 14;

    /// <summary>
    /// How near a player a mob has to be to be counted against the caps above. Deliberately not the whole
    /// loaded world: an animal left behind half a mile back would otherwise go on holding a slot open, and a
    /// player walking into fresh country would find it empty because their old herd still filled the cap.
    /// </summary>
    private const float MobCapDistance = 56F;

    /// <summary>
    /// How many rounds a hostile mob may stand in the sun before it burns up. A round is a second, so a
    /// night's worth of them is gone shortly after sunrise rather than still wandering the fields at noon.
    /// <para>
    /// Not instant, and not conditional on nobody watching. Clearing them the moment the sun came up would
    /// have them blink out in front of whoever they were chasing, and sparing the ones being watched, which
    /// is what this used to do, spared exactly the ones the player could see: a zombie follows its target, so
    /// the ones near enough to be excused were the ones that then walked about in broad daylight all day.
    /// </para>
    /// </summary>
    private const int SunlightRoundsBeforeBurningUp = 8;

    /// <summary>Mobs never appear closer to a player than this, so nothing is seen popping into existence.</summary>
    private const float MinDistanceFromPlayer = 14F;

    /// <summary>Nor further off than this, which keeps them inside the area somebody has loaded.</summary>
    private const float MaxDistanceFromPlayer = 44F;

    /// <summary>A hostile mob further than this from every player is removed again.</summary>
    private const float HostileDespawnDistance = 72F;

    /// <summary>
    /// The same for animals, and much further out, because a herd is meant to still be grazing where it was
    /// left rather than to have evaporated the moment its field went out of sight. Nothing writes mobs to
    /// disk, so they cannot be kept for good the way the game they are borrowed from keeps them; this is as
    /// close as a world that forgets its animals on shutdown can come.
    /// </summary>
    private const float AnimalDespawnDistance = 112F;

    /// <summary>Blocks of clear space a mob needs above the ground to fit.</summary>
    private const int RequiredHeadroomBlocks = 2;

    /// <summary>How far down from the recorded top of a column the surface is searched for.</summary>
    private const int SurfaceSearchDepth = 12;

    /// <summary>
    /// How far down from a sampled height a floor is searched for underground. Short, so that a sample taken
    /// in the middle of solid rock is thrown away rather than dropped into whatever cavern lies far below it.
    /// </summary>
    private const int UndergroundSearchDepth = 10;

    /// <summary>How far from the spot a pack was anchored on its members may be placed.</summary>
    private const int PackSpreadBlocks = 4;

    /// <summary>
    /// How near a block that gives off light keeps hostile mobs away. Stands in for a light level the server
    /// does not have, so a lit room and a lit tunnel are both left alone.
    /// </summary>
    private const int LightSourceExclusionRadius = 12;

    /// <summary>
    /// How many blocks the daylight carries once it has stopped falling straight down, which is how far from
    /// the open sky a spot has to be to be dark by day.
    /// <para>
    /// Sunlight comes down an unobstructed column at full strength and then spreads, losing a step for every
    /// block it travels sideways and for every block it drops below the floor of the column it came down.
    /// The renderer's flood fill starts it at fifteen and mobs want under eight, which leaves seven steps;
    /// see <see cref="IsReachedBySunlight"/>, which measures that distance rather than reading a light map
    /// the server does not keep.
    /// </para>
    /// <para>
    /// Spreading is the whole point of doing it this way. Asking only whether a spot had something overhead
    /// would call the ground under a tree sheltered, and it is broad daylight there however much of the sky
    /// the leaves take up, because the light walks in from the open ground a few blocks away.
    /// </para>
    /// </summary>
    private const int SunlightSpreadDistance = 7;

    /// <summary>
    /// How far down a column the search for the block that stops the daylight goes before giving up. Only
    /// deep water and glass towers run longer than this, and a column that does is treated as sunlit all the
    /// way down, which costs a spawn rather than allowing a wrong one.
    /// </summary>
    private const int SunlightColumnSearchDepth = 32;

    /// <summary>
    /// One kind of mob a round may produce, how likely it is against the others, and how many of it go in at
    /// once. The weights are the ones the game they come from uses, which is what makes sheep the animal
    /// seen most often and cows the one seen least.
    /// </summary>
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

    /// <summary>
    /// How many rounds running each hostile mob has been standing in the sun, keyed by entity id. Rebuilt
    /// from scratch every round into <see cref="_nextSunlightRounds"/> and swapped, so a mob that has died or
    /// stepped back into the shade leaves no entry behind: entity ids are handed out again after a despawn,
    /// and a count left lying about would be inherited by whatever is given the id next.
    /// </summary>
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

        // Both kinds are tried on the same round rather than the hour choosing between them, so the animals
        // are still standing in their field after dark instead of the world emptying of everything but what
        // came out at sunset.
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

        // Swapped rather than assigned, so the pair of dictionaries is reused round after round.
        (_sunlightRounds, _nextSunlightRounds) = (_nextSunlightRounds, _sunlightRounds);

        // Collected first, because despawning removes the entity from the collection being walked.
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

    /// <summary>
    /// Whether a hostile mob has now stood in the daylight long enough to burn up. Removed outright rather
    /// than burned down through the health a punch takes off: nobody is watching a health bar, and a mob
    /// that vanished after a few seconds of sun and one that was whittled away over them look the same from
    /// where the player stands. What matters is that it happens wherever the sun reaches, so that a cave
    /// keeps whatever came out in it however bright the day above has become, and nothing else does.
    /// </summary>
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

    private void TrySpawnRound(WorldServer world, SpawnEntry[] spawns, bool hostile, int attempts)
    {
        int mobCount = CountMobsNearPlayers(world, hostile);
        int mobLimit = (hostile ? MaxHostilesPerPlayer : MaxAnimalsPerPlayer) * _players.Count;

        for (int attempt = 0; attempt < attempts && mobCount < mobLimit; attempt++)
        {
            mobCount += TrySpawnPack(world, spawns, hostile);
        }
    }

    /// <summary>
    /// How many mobs of the given kind are standing near a player. See <see cref="MobCapDistance"/> for why
    /// the ones further out are not counted.
    /// </summary>
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

    /// <summary>
    /// Looks for somewhere a pack could go, and fills it if it finds one. Returns how many mobs went in,
    /// which is zero for an attempt that found nowhere suitable.
    /// </summary>
    private int TrySpawnPack(WorldServer world, SpawnEntry[] spawns, bool hostile)
    {
        Chunk chunk = _spawnableChunks[Random.Shared.Next(_spawnableChunks.Count)];
        int anchorX = chunk.GridX * 16 + Random.Shared.Next(16);
        int anchorZ = chunk.GridZ * 16 + Random.Shared.Next(16);

        int columnTop = GetColumnTop(chunk, anchorX, anchorZ);

        // After dark half the hostile attempts are aimed at the surface and half at a random depth under it,
        // so a night above ground is busy without the caves beneath it staying empty. While there is light in
        // the sky the surface has nothing to offer them, so every attempt goes underground. Animals belong on
        // the surface and nowhere else, so theirs always start at the top of the column.
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

        // The anchor is known good, so the pack always has at least one member. The rest look for ground of
        // their own nearby and are simply left out where there is none, which is what makes a pack squeezed
        // against a cliff or a cave wall come out smaller than one in an open field.
        int spawned = Spawn(world, entry.Type, anchorX, anchorFeetY, anchorZ) ? 1 : 0;

        for (int member = 1; member < packSize; member++)
        {
            int memberX = anchorX + Random.Shared.Next(-PackSpreadBlocks, PackSpreadBlocks + 1);
            int memberZ = anchorZ + Random.Shared.Next(-PackSpreadBlocks, PackSpreadBlocks + 1);

            // Started a little above the anchor rather than at the top of the column, so a pack that appeared
            // in a tunnel stays in it instead of half of it landing on the hillside overhead.
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

    /// <summary>Picks a kind of mob at random, in proportion to the weights on the given table.</summary>
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

    /// <summary>
    /// Everything that has to hold before a mob may be built at a column: there has to be a floor with room
    /// to stand on it, it has to be a floor of the right sort, it has to be dark if the mob is one of the
    /// ones that needs darkness, and it has to be at a distance from every player that is neither startling
    /// nor pointless.
    /// </summary>
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

        // Offset to the middle of the block, so the mob is not measured from the corner it will be standing
        // half a body away from.
        return IsAtUsableDistanceFromPlayers(new Vector3(worldX + 0.5F, feetY, worldZ + 0.5F));
    }

    /// <summary>
    /// Builds a mob and puts it in the world facing whichever way it happened to come out, so that a pack
    /// which appeared together is not standing to attention in the same direction.
    /// </summary>
    private static bool Spawn(WorldServer world, EntityType entityType, int worldX, int feetY, int worldZ)
    {
        var feet = new Vector3(worldX + 0.5F, feetY, worldZ + 0.5F);

        // Built last, because building one takes an entity id that would otherwise have to be handed back.
        Mob? mob = MobFactory.Create(entityType, world.GenerateEntityId(), world, feet);
        if (mob is null)
        {
            return false;
        }

        mob.Yaw = Random.Shared.NextSingle() * MathF.Tau;
        world.SpawnEntity(mob);
        return true;
    }

    /// <summary>
    /// The height of the highest block in a column, which is an upper bound on where its surface is rather
    /// than the surface itself: the record only ever grows as blocks are added.
    /// </summary>
    private static int GetColumnTop(Chunk chunk, int worldX, int worldZ)
    {
        return Math.Min(
            chunk.TopMostBlocks[worldX & 15, worldZ & 15],
            Constants.MAX_BUILD_HEIGHT - RequiredHeadroomBlocks - 1);
    }

    /// <summary>
    /// Finds where a mob's feet would rest in a column, searching downwards from the given height for the
    /// first open block with something solid under it. Searching downwards is what puts a mob on top of what
    /// it finds rather than inside it, and starting from the top of the column is what confines a search to
    /// the surface, since the first floor met on the way down from there is the highest there is.
    /// </summary>
    private static bool TryFindFloor(World world, int worldX, int worldZ, int startY, int searchDepth, out int feetY)
    {
        feetY = 0;

        int highest = Math.Min(startY, Constants.MAX_BUILD_HEIGHT - RequiredHeadroomBlocks - 1);

        // Never down to the floor of the world itself, so that the block under a mob's feet always exists.
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

    /// <summary>
    /// Whether a mob could stand where this block is. Flowers and grass have no collision box, so they are
    /// passable and a field of them is still somewhere a mob can appear.
    /// </summary>
    private static bool IsPassable(World world, Vector3i blockPos)
    {
        BlockState state = world.GetBlockAt(blockPos);
        Block block = state.GetBlock();

        // Water stops nothing and so would read as passable, but a seabed is not somewhere a sheep grazes
        // or a zombie walks. Nothing in the world swims, so being in water at all rules a spot out.
        if (block.IsLiquid)
        {
            return false;
        }

        return block == BlockRegistry.Air || block.GetCollisionBox(state, blockPos).Length == 0;
    }

    /// <summary>
    /// Whether the block under a spot is one a mob of this kind will stand on.
    /// <para>
    /// Animals want grass, and that one rule does the work a table of biomes would: a desert is sand, a
    /// mountain is bare stone and a summit is snow, so none of them grows livestock, while every stretch of
    /// green in the world does. Hostile mobs are not so particular and take any solid block that light does
    /// not pass through, which rules out a cactus and leaves them everything else.
    /// </para>
    /// </summary>
    private static bool IsGroundMobsWillStandOn(World world, Vector3i groundPos, bool hostile)
    {
        Block block = world.GetBlockAt(groundPos).GetBlock();

        if (!hostile)
        {
            return block == BlockRegistry.Grass || block == BlockRegistry.SnowyGrass;
        }

        return block.IsOpaque;
    }

    /// <summary>
    /// Whether a spot is dark enough for a hostile mob. Standing in for a light level the server does not
    /// have: while the sky is dark the whole of the outdoors qualifies, and while there is any light in it
    /// only what the sun cannot reach does. Near anything that glows, nothing qualifies at either hour.
    /// </summary>
    private static bool IsDarkEnoughForHostiles(World world, Vector3i feet)
    {
        if (!world.Environment.IsDarkOutside && IsReachedBySunlight(world, feet))
        {
            return false;
        }

        return !IsNearLightSource(world, feet);
    }

    /// <summary>
    /// Whether the daylight falls on a spot. The sun comes down every column that has nothing solid over it
    /// and then spreads out from where it lands, so a spot is lit when any such column is within
    /// <see cref="SunlightSpreadDistance"/> steps of it, counting a step for each block sideways and each
    /// block down. That is the renderer's flood fill measured rather than run, and it is what tells a cave
    /// from the shade of a tree: both have something overhead, but only one of them is out of the sun's
    /// reach.
    /// <para>
    /// Walls are not accounted for, so a windowless room with the open ground just outside it reads as lit
    /// and nothing appears in it. That is the safe way round to be wrong: it withholds a spawn rather than
    /// putting a zombie somewhere the player can see the sun.
    /// </para>
    /// </summary>
    private static bool IsReachedBySunlight(World world, Vector3i blockPos)
    {
        // The column the spot is in, first and on its own, because it settles every spot standing in the
        // open, which is most of them, without the search below having to run at all.
        if (IsSunlitColumnWithinReach(world, blockPos, offsetX: 0, offsetZ: 0))
        {
            return true;
        }

        for (int offsetX = -SunlightSpreadDistance; offsetX <= SunlightSpreadDistance; offsetX++)
        {
            // Whatever the sideways budget has left after this column is how far the search may go the other
            // way, which is what makes the area searched a diamond rather than a square.
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

    /// <summary>
    /// Whether the daylight coming down one neighbouring column still has the strength to reach the spot,
    /// having travelled the distance between the two.
    /// </summary>
    private static bool IsSunlitColumnWithinReach(World world, Vector3i blockPos, int offsetX, int offsetZ)
    {
        int worldX = blockPos.X + offsetX;
        int worldZ = blockPos.Z + offsetZ;

        Vector2 chunkPos = World.GetChunkPosition(worldX, worldZ);

        // A column in a chunk nobody has loaded is left out rather than guessed at. The spot's own column is
        // always loaded, so the open sky directly overhead is never missed this way.
        if (!world.LoadedChunks.TryGetValue(chunkPos, out Chunk? chunk))
        {
            return false;
        }

        int sunlitFloorY = GetSunlitFloorOfColumn(chunk, worldX & 15, worldZ & 15);

        int distance = Math.Abs(offsetX) + Math.Abs(offsetZ) + Math.Max(0, sunlitFloorY - blockPos.Y);
        return distance <= SunlightSpreadDistance;
    }

    /// <summary>
    /// The lowest block of a column the daylight still falls straight into, which is the one above the
    /// highest block that stops light. Leaves and stone both stop it; flowers, glass and water do not, so a
    /// pond is lit to its bed and a meadow to the ground.
    /// </summary>
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

    /// <summary>
    /// Whether anything giving off light stands within <see cref="LightSourceExclusionRadius"/> of the spot.
    /// Only the chunk the spot is in and the ring around it are searched, which is enough: the radius is
    /// shorter than a chunk is wide, so nothing further out could reach it.
    /// </summary>
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
