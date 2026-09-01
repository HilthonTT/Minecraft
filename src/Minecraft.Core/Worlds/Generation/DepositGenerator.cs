using Minecraft.Core.Worlds.Blocks;
using Minecraft.Core.Worlds.Chunks;

namespace Minecraft.Core.Worlds.Generation;

public sealed class DepositGenerator
{
    private const uint DepositSalt = 0x4F52_4553u;

    private const int MaxVeinReach = 12;

    private readonly record struct Deposit(
        Block Block,
        float VeinsPerChunk,
        int MinBlocks,
        int MaxBlocks,
        int MinY,
        int MaxY);

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

        new(BlockRegistry.Glowstone, VeinsPerChunk: 0.35F, MinBlocks: 5, MaxBlocks: 11, MinY: 5, MaxY: 40),
    ];

    private readonly int _seed;

    public DepositGenerator(int seed)
    {
        _seed = seed;
    }

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

    private void PlaceVeinsOwnedBy(Chunk target, int ownerChunkX, int ownerChunkZ)
    {
        for (int i = 0; i < _deposits.Length; i++)
        {
            Deposit deposit = _deposits[i];

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

    private static bool IsOutOfReach(Chunk target, int originX, int originZ)
    {
        int minX = target.GridX * 16;
        int minZ = target.GridZ * 16;

        return originX < minX - MaxVeinReach || originX > minX + 15 + MaxVeinReach ||
               originZ < minZ - MaxVeinReach || originZ > minZ + 15 + MaxVeinReach;
    }

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

            float taper = MathF.Sin(along * MathF.PI);
            float radius = ((blockCount / 16F) + 0.6F) * taper;

            FillSphere(target, state, centreX, centreY, centreZ, radius);
        }
    }

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
