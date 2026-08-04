using Minecraft.Core.Worlds.Blocks;
using Minecraft.Core.Worlds.Chunks;

namespace Minecraft.Core.Worlds.Generation;

/// <summary>
/// Salts the stone with everything that is not stone: ores, and the pockets of dirt, gravel and clay that
/// keep the underground from being one flat grey.
/// <para>
/// A vein belongs to the chunk its centre falls in, but it is free to spill over into the ones around it. So
/// that a vein comes out whole rather than sheared off at a chunk border, a chunk lays down not only its own
/// veins but also those of its eight neighbours, keeping whichever blocks land inside itself. Every vein is
/// drawn from a seed made of the world seed and its owning chunk, so both chunks that share one lay down the
/// same vein and agree on where it goes.
/// </para>
/// </summary>
public sealed class DepositGenerator
{
    /// <summary>Mixed into the seed so deposits do not land wherever some other feature was placed.</summary>
    private const uint DepositSalt = 0x4F52_4553u;

    /// <summary>
    /// How far a vein may reach from its centre. Kept under a chunk, which is what lets a chunk find every
    /// vein reaching into it by looking no further than its immediate neighbours.
    /// </summary>
    private const int MaxVeinReach = 12;

    /// <summary>
    /// One kind of thing buried in the stone.
    /// </summary>
    /// <param name="Block">What the vein is made of.</param>
    /// <param name="VeinsPerChunk">
    /// How many veins a chunk gets on average. Fractions are honoured, so a value below one gives a vein to
    /// that share of chunks and leaves the rest without.
    /// </param>
    /// <param name="MinBlocks">The fewest blocks a vein is drawn with.</param>
    /// <param name="MaxBlocks">The most blocks a vein is drawn with, which also sets how fat it grows.</param>
    /// <param name="MinY">The lowest a vein's centre may sit.</param>
    /// <param name="MaxY">The highest a vein's centre may sit.</param>
    private readonly record struct Deposit(
        Block Block,
        float VeinsPerChunk,
        int MinBlocks,
        int MaxBlocks,
        int MinY,
        int MaxY);

    /// <summary>
    /// Ordered from the most common to the rarest, and from the shallowest to the deepest. The order matters
    /// only in that it must never change: it is what each deposit's seed is derived from, so reordering the
    /// table would move every vein in every existing world.
    /// </summary>
    private readonly Deposit[] _deposits =
    [
        new(BlockRegistry.Dirt, VeinsPerChunk: 7, MinBlocks: 20, MaxBlocks: 34, MinY: 6, MaxY: 100),
        new(BlockRegistry.Gravel, VeinsPerChunk: 6, MinBlocks: 20, MaxBlocks: 34, MinY: 6, MaxY: 90),
        new(BlockRegistry.CoalOre, VeinsPerChunk: 11, MinBlocks: 8, MaxBlocks: 18, MinY: 6, MaxY: 120),
        new(BlockRegistry.IronOre, VeinsPerChunk: 7, MinBlocks: 5, MaxBlocks: 10, MinY: 4, MaxY: 64),
        new(BlockRegistry.Clay, VeinsPerChunk: 1.5F, MinBlocks: 10, MaxBlocks: 20, MinY: 40, MaxY: 70),
        new(BlockRegistry.RedstoneOre, VeinsPerChunk: 3, MinBlocks: 5, MaxBlocks: 9, MinY: 2, MaxY: 18),
        new(BlockRegistry.GoldOre, VeinsPerChunk: 2, MinBlocks: 4, MaxBlocks: 8, MinY: 2, MaxY: 32),
        new(BlockRegistry.DiamondOre, VeinsPerChunk: 1, MinBlocks: 3, MaxBlocks: 7, MinY: 2, MaxY: 15),

        // The only light the world generates with. Buried like anything else, so it is a cave breaking into a
        // vein that puts a lit pocket underground rather than the generator placing one deliberately.
        new(BlockRegistry.Glowstone, VeinsPerChunk: 0.35F, MinBlocks: 5, MaxBlocks: 11, MinY: 5, MaxY: 40),
    ];

    private readonly int _seed;

    public DepositGenerator(int seed)
    {
        _seed = seed;
    }

    /// <summary>
    /// Buries every vein that reaches into the chunk.
    /// </summary>
    /// <param name="chunk">
    /// A chunk that has been filled with terrain but not yet carved. Veins only ever replace stone, so laying
    /// them before the caves is what lets a cave cut through one and leave its face showing.
    /// </param>
    public void PlaceDepositsIn(Chunk chunk)
    {
        for (int neighbourX = -1; neighbourX <= 1; neighbourX++)
        {
            for (int neighbourZ = -1; neighbourZ <= 1; neighbourZ++)
            {
                PlaceVeinsOwnedBy(chunk, chunk.GridX + neighbourX, chunk.GridZ + neighbourZ);
            }
        }
    }

    /// <summary>
    /// Lays down every vein belonging to one chunk, keeping only the blocks that land inside
    /// <paramref name="target"/>. Called with the target's own position as well as its neighbours'.
    /// </summary>
    private void PlaceVeinsOwnedBy(Chunk target, int ownerChunkX, int ownerChunkZ)
    {
        for (int i = 0; i < _deposits.Length; i++)
        {
            Deposit deposit = _deposits[i];

            // A stream of its own per deposit, so that changing how common one of them is does not shuffle
            // where all the others ended up.
            var random = new Random(GetDepositSeed(ownerChunkX, ownerChunkZ, i));

            int veinCount = (int)deposit.VeinsPerChunk;
            if (random.NextSingle() < deposit.VeinsPerChunk - veinCount)
            {
                veinCount++;
            }

            for (int vein = 0; vein < veinCount; vein++)
            {
                int originX = ownerChunkX * 16 + random.Next(16);
                int originZ = ownerChunkZ * 16 + random.Next(16);
                int originY = random.Next(deposit.MinY, deposit.MaxY + 1);

                // Drawn from the same stream whether or not the vein is kept, so that a vein being out of
                // reach does not change where the next one in the chunk lands.
                int blockCount = random.Next(deposit.MinBlocks, deposit.MaxBlocks + 1);
                float angle = random.NextSingle() * MathF.Tau;
                float endYOffset = random.Next(-2, 3);

                if (IsOutOfReach(target, originX, originZ))
                {
                    continue;
                }

                DrawVein(target, deposit.Block, originX, originY, originZ, blockCount, angle, endYOffset);
            }
        }
    }

    /// <summary>Whether a vein centred here could not possibly reach into the chunk being generated.</summary>
    private static bool IsOutOfReach(Chunk target, int originX, int originZ)
    {
        int minX = target.GridX * 16;
        int minZ = target.GridZ * 16;

        return originX < minX - MaxVeinReach || originX > minX + 15 + MaxVeinReach ||
               originZ < minZ - MaxVeinReach || originZ > minZ + 15 + MaxVeinReach;
    }

    /// <summary>
    /// Draws one vein as a short slanted line of overlapping spheres, fattest in the middle and tapering to
    /// nothing at both ends. That is what gives a vein a lens shape rather than the ball a single sphere
    /// would leave, and what lets a large one still come to a point instead of ending in a wall.
    /// </summary>
    private static void DrawVein(
        Chunk target,
        Block block,
        int originX,
        int originY,
        int originZ,
        int blockCount,
        float angle,
        float endYOffset)
    {
        // A longer vein for a larger one, so that growing a deposit stretches it out rather than only
        // swelling it in place.
        float halfLength = (blockCount / 8F) + 1F;

        float startX = originX - (MathF.Sin(angle) * halfLength);
        float startZ = originZ - (MathF.Cos(angle) * halfLength);
        float endX = originX + (MathF.Sin(angle) * halfLength);
        float endZ = originZ + (MathF.Cos(angle) * halfLength);
        float startY = originY - (endYOffset / 2F);
        float endY = originY + (endYOffset / 2F);

        BlockState state = BlockRegistry.GetState(block);

        for (int step = 0; step < blockCount; step++)
        {
            float along = (step + 0.5F) / blockCount;

            float centreX = startX + ((endX - startX) * along);
            float centreY = startY + ((endY - startY) * along);
            float centreZ = startZ + ((endZ - startZ) * along);

            // Zero at both ends and widest halfway along.
            float taper = MathF.Sin(along * MathF.PI);
            float radius = ((blockCount / 16F) + 0.6F) * taper;

            FillSphere(target, state, centreX, centreY, centreZ, radius);
        }
    }

    /// <summary>
    /// Replaces the stone inside a sphere. Only stone: anywhere else the sphere reaches is either soil the
    /// surface is made of or air a cave already took, and a vein has no business showing up in either.
    /// </summary>
    private static void FillSphere(Chunk target, BlockState state, float centreX, float centreY, float centreZ, float radius)
    {
        if (radius <= 0F)
        {
            return;
        }

        int minX = Math.Max((int)MathF.Floor(centreX - radius), target.GridX * 16);
        int maxX = Math.Min((int)MathF.Ceiling(centreX + radius), (target.GridX * 16) + 15);
        int minZ = Math.Max((int)MathF.Floor(centreZ - radius), target.GridZ * 16);
        int maxZ = Math.Min((int)MathF.Ceiling(centreZ + radius), (target.GridZ * 16) + 15);
        int minY = Math.Max((int)MathF.Floor(centreY - radius), 0);
        int maxY = Math.Min((int)MathF.Ceiling(centreY + radius), Constants.MAX_BUILD_HEIGHT - 1);

        float radiusSquared = radius * radius;

        for (int worldX = minX; worldX <= maxX; worldX++)
        {
            float dx = worldX + 0.5F - centreX;

            for (int worldZ = minZ; worldZ <= maxZ; worldZ++)
            {
                float dz = worldZ + 0.5F - centreZ;

                for (int y = minY; y <= maxY; y++)
                {
                    float dy = y + 0.5F - centreY;

                    if ((dx * dx) + (dy * dy) + (dz * dz) > radiusSquared)
                    {
                        continue;
                    }

                    int localX = worldX & 15;
                    int localZ = worldZ & 15;

                    if (target.GetBlockAt(localX, y, localZ).GetBlock() != BlockRegistry.Stone)
                    {
                        continue;
                    }

                    target.AddBlockAt(localX, y, localZ, state);
                }
            }
        }
    }

    /// <summary>
    /// Mixes the world seed, a chunk position and which deposit it is into a seed for that chunk's veins of
    /// that deposit. Deliberately not <see cref="HashCode"/>, which is randomised per process and would
    /// scatter a world's ores differently every time it was loaded.
    /// </summary>
    private int GetDepositSeed(int chunkX, int chunkZ, int depositIndex)
    {
        unchecked
        {
            uint hash = (uint)_seed ^ DepositSalt;
            hash = (hash ^ (uint)chunkX) * 2654435761u;
            hash = (hash ^ (uint)chunkZ) * 2246822519u;
            hash = (hash ^ (uint)depositIndex) * 3266489917u;
            hash ^= hash >> 15;
            hash *= 2246822519u;
            hash ^= hash >> 13;
            return (int)hash;
        }
    }
}
