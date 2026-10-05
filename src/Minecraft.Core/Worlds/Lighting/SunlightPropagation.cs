using Minecraft.Core.Utilities;
using Minecraft.Core.Utilities.Vectors;
using Minecraft.Core.Worlds.Chunks;
using OpenTK.Mathematics;

namespace Minecraft.Core.Worlds.Lighting;

public static class SunlightPropagation
{
    private const uint FullIntensity = 15;

    private const uint TopSectionIndex = 15;

    public static Chunk[] RepairOnBlockRemoved(World world, Chunk chunk, Vector3i blockPos)
    {
        return RepairOnBlockAdded(world, chunk, blockPos).Where(c => c != chunk).ToArray();
    }

    public static Chunk[] GenerateInitialGrid(World world, Chunk chunk)
    {
        var lightPropagationQueue = new Queue<LightAddNode>();

        uint lowestEmptySection = chunk.GetLowestEmptySectionAfterEachOtherFromTop();
        uint lowestEmptyHeight = lowestEmptySection * 16;

        if (lowestEmptySection == TopSectionIndex)
        {
            SeedTopFace(chunk, lightPropagationQueue, edgesOnly: false);
        }
        else
        {
            for (uint x = 1; x < 15; x++)
            {
                for (uint y = lowestEmptyHeight + 1; y < Constants.MAX_BUILD_HEIGHT; y++)
                {
                    for (uint z = 1; z < 15; z++)
                    {
                        chunk.LightMap.SetSunLightIntensityAt(x, y, z, FullIntensity);
                    }
                }
            }

            for (int x = 0; x < 16; x++)
            {
                for (int z = 0; z < 16; z++)
                {
                    Vector3i chunkLocalPos = new Vector3i(x, (int)lowestEmptyHeight, z).ToChunkLocal();
                    chunk.LightMap.SetSunLightIntensityAt(chunkLocalPos, FullIntensity);
                    lightPropagationQueue.Enqueue(new LightAddNode(chunk, chunkLocalPos));
                }
            }

            SeedTopFace(chunk, lightPropagationQueue, edgesOnly: true);
        }

        var updatedChunks = new HashSet<Chunk>(PropagateLight(world, chunk, lightPropagationQueue));

        SeedFromNeighbourBorders(world, chunk, lightPropagationQueue);
        updatedChunks.UnionWith(PropagateLight(world, chunk, lightPropagationQueue));

        updatedChunks.Remove(chunk);
        return updatedChunks.ToArray();
    }

    private static void SeedTopFace(Chunk chunk, Queue<LightAddNode> lightPropagationQueue, bool edgesOnly)
    {
        for (int x = 0; x < 16; x++)
        {
            for (int z = 0; z < 16; z++)
            {
                if (edgesOnly && x != 0 && x != 15 && z != 0 && z != 15)
                {
                    continue;
                }

                if (chunk.GetBlockAt(x, Constants.MAX_BUILD_HEIGHT - 1, z).GetBlock().IsOpaque)
                {
                    continue;
                }

                Vector3i chunkLocalPos = new Vector3i(x, Constants.MAX_BUILD_HEIGHT - 1, z).ToChunkLocal();
                chunk.LightMap.SetSunLightIntensityAt(chunkLocalPos, FullIntensity);
                lightPropagationQueue.Enqueue(new LightAddNode(chunk, chunkLocalPos));
            }
        }
    }

    private static void SeedFromNeighbourBorders(World world, Chunk chunk, Queue<LightAddNode> lightPropagationQueue)
    {
        foreach (Chunk neighbour in world.GetCardinalChunks(chunk))
        {
            int offsetX = neighbour.GridX - chunk.GridX;
            int offsetZ = neighbour.GridZ - chunk.GridZ;

            for (int i = 0; i < 16; i++)
            {
                int neighbourX = offsetX < 0 ? 15 : offsetX > 0 ? 0 : i;
                int neighbourZ = offsetZ < 0 ? 15 : offsetZ > 0 ? 0 : i;
                int ownX = offsetX < 0 ? 0 : offsetX > 0 ? 15 : i;
                int ownZ = offsetZ < 0 ? 0 : offsetZ > 0 ? 15 : i;

                for (int y = 0; y < Constants.MAX_BUILD_HEIGHT; y++)
                {
                    var neighbourPos = new Vector3i(neighbourX, y, neighbourZ);
                    uint neighbourLight = neighbour.LightMap.GetSunLightIntensityAt(neighbourPos);
                    uint ownLight = chunk.LightMap.GetSunLightIntensityAt((uint)ownX, (uint)y, (uint)ownZ);

                    if (neighbourLight > ownLight + 1)
                    {
                        lightPropagationQueue.Enqueue(new LightAddNode(neighbour, neighbourPos));
                    }
                }
            }
        }
    }

    public static Chunk[] RepairOnBlockAdded(World world, Chunk chunk, Vector3i blockPos)
    {
        var updatedChunks = new HashSet<Chunk>();

        Vector3i chunkLocalPos = blockPos.ToChunkLocal();
        Queue<LightRemoveNode> darknessPropagationQueue = new();
        Queue<LightAddNode> lightPropagationQueue = new();

        uint currentLightValue = chunk.LightMap.GetSunLightIntensityAt(chunkLocalPos);
        darknessPropagationQueue.Enqueue(new LightRemoveNode(chunk, chunkLocalPos, currentLightValue));
        chunk.LightMap.SetSunLightIntensityAt(chunkLocalPos, 0);

        updatedChunks.UnionWith(
            PropagateDarkness(world, chunk, darknessPropagationQueue, lightPropagationQueue));

        if (chunkLocalPos.Y == Constants.MAX_BUILD_HEIGHT - 1 && !chunk.GetBlockAt(chunkLocalPos).GetBlock().IsOpaque)
        {
            chunk.LightMap.SetSunLightIntensityAt(chunkLocalPos, FullIntensity);
            lightPropagationQueue.Enqueue(new LightAddNode(chunk, chunkLocalPos));
        }

        updatedChunks.UnionWith(PropagateLight(world, chunk, lightPropagationQueue));

        updatedChunks.Remove(chunk);
        return updatedChunks.ToArray();
    }

    private static HashSet<Chunk> PropagateDarkness(
        World world,
        Chunk chunk,
        Queue<LightRemoveNode> darkQueue,
        Queue<LightAddNode> lightQueue)
    {
        HashSet<Chunk> processedChunks = new();

        while (darkQueue.Count != 0)
        {
            LightRemoveNode lightRemoveNode = darkQueue.Dequeue();

            Vector3i[] neighbourPositions = lightRemoveNode.ChunkLocalPos.GetSurroundingPositions();
            for (int i = 0; i < neighbourPositions.Length; i++)
            {
                (Vector3i position, Chunk currentChunk) = BlockPropagation.FixReference(world, neighbourPositions[i],
                    lightRemoveNode.Chunk, out bool referenceFixable);

                if (!referenceFixable || currentChunk.GetBlockAt(position).GetBlock().IsOpaque)
                {
                    continue;
                }

                uint neighbourLight = currentChunk.LightMap.GetSunLightIntensityAt(position);

                if ((neighbourLight != 0 && neighbourLight < lightRemoveNode.LightValue) ||
                    (lightRemoveNode.LightValue == FullIntensity && lightRemoveNode.ChunkLocalPos.Down() == position))
                {
                    currentChunk.LightMap.SetSunLightIntensityAt(position, 0);
                    darkQueue.Enqueue(new LightRemoveNode(currentChunk, position, neighbourLight));

                    MarkProcessed(processedChunks, chunk, currentChunk);
                }
                else if (neighbourLight >= lightRemoveNode.LightValue)
                {
                    lightQueue.Enqueue(new LightAddNode(currentChunk, position));

                    MarkProcessed(processedChunks, chunk, currentChunk);
                }
            }
        }

        return processedChunks;
    }

    private static HashSet<Chunk> PropagateLight(World world, Chunk chunk, Queue<LightAddNode> queue)
    {
        HashSet<Chunk> processedChunks = new();

        while (queue.Count != 0)
        {
            LightAddNode lightAddNode = queue.Dequeue();
            Vector3i chunkLocalPos = lightAddNode.ChunkLocalPos;

            uint currentLight = lightAddNode.Chunk.LightMap.GetSunLightIntensityAt(chunkLocalPos);
            if (currentLight <= 1)
            {
                continue;
            }

            Vector3i[] neighbourPositions = currentLight == FullIntensity ?
                chunkLocalPos.GetSurroundingPositionsBesidesUp() :
                chunkLocalPos.GetSurroundingPositions();
            for (int i = 0; i < neighbourPositions.Length; i++)
            {
                (Vector3i position, Chunk currentChunk) = BlockPropagation.FixReference(world, neighbourPositions[i],
                    lightAddNode.Chunk, out bool referenceFixable);

                if (!referenceFixable || currentChunk.GetBlockAt(position).GetBlock().IsOpaque)
                {
                    continue;
                }

                if (chunkLocalPos.Down() == position && currentLight == FullIntensity)
                {
                    currentChunk.LightMap.SetSunLightIntensityAt(position, currentLight);
                    queue.Enqueue(new LightAddNode(currentChunk, position));

                    MarkProcessed(processedChunks, chunk, currentChunk);
                }
                else if (currentChunk.LightMap.GetSunLightIntensityAt(position) < currentLight - 1)
                {
                    currentChunk.LightMap.SetSunLightIntensityAt(position, currentLight - 1);
                    queue.Enqueue(new LightAddNode(currentChunk, position));

                    MarkProcessed(processedChunks, chunk, currentChunk);
                }
            }
        }

        return processedChunks;
    }

    private static void MarkProcessed(HashSet<Chunk> processedChunks, Chunk sourceChunk, Chunk currentChunk)
    {
        if (currentChunk != sourceChunk)
        {
            processedChunks.Add(currentChunk);
        }
    }
}
