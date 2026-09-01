using Minecraft.Core.Utilities.Vectors;
using Minecraft.Core.Worlds.Blocks;
using Minecraft.Core.Worlds.Chunks;
using OpenTK.Mathematics;

namespace Minecraft.Core.Worlds.Lighting;

public static class BlockLightPropagation
{
    public static Chunk[] RepairOnBlockRemoved(World world, Chunk chunk, Vector3i blockPos)
    {
        Queue<LightRemoveNode> darknessPropagationQueue = new();
        Queue<LightAddNode> lightPropagationQueue = new();

        Vector3i sourceChunkLocalPos = blockPos.ToChunkLocal();

        HashSet<Chunk> updatedChunks = new();

        foreach (LightChannel channel in LightUtils.BlockVisibileColorChannels)
        {
            ClearChannelAt(chunk, sourceChunkLocalPos, channel, darknessPropagationQueue);

            updatedChunks.UnionWith(
                PropagateDarkness(world, chunk, darknessPropagationQueue, lightPropagationQueue, channel));

            foreach (KeyValuePair<Vector3i, BlockState> kp in chunk.LightSourceBlocks)
            {
                if (kp.Value is not ILightSource lightSourceInChunk)
                {
                    continue;
                }

                Vector3i chunkLocalPos = kp.Key.ToChunkLocal();
                uint lightValue = LightUtils.GetChannelColor(lightSourceInChunk, channel);
                LightUtils.SetLightOfChannel(chunk, chunkLocalPos, channel, lightValue);
                lightPropagationQueue.Enqueue(new LightAddNode(chunk, chunkLocalPos));
            }

            updatedChunks.UnionWith(PropagateLight(world, chunk, lightPropagationQueue, channel));

            lightPropagationQueue.Clear();
            darknessPropagationQueue.Clear();
        }

        updatedChunks.Remove(chunk);
        return updatedChunks.ToArray();
    }

    public static Chunk[] RepairOnBlockAdded(World world, Chunk chunk, Vector3i blockPos, BlockState blockState)
    {
        if (blockState is not ILightSource lightSource)
        {
            return RepairOnBlockRemoved(world, chunk, blockPos);
        }

        HashSet<Chunk> updatedChunks = new();

        Vector3i sourceChunkLocalPos = blockPos.ToChunkLocal();

        Queue<LightRemoveNode> darknessPropagationQueue = new();
        Queue<LightAddNode> lightPropagationQueue = new();

        foreach (LightChannel channel in LightUtils.BlockVisibileColorChannels)
        {
            ClearChannelAt(chunk, sourceChunkLocalPos, channel, darknessPropagationQueue);

            updatedChunks.UnionWith(
                PropagateDarkness(world, chunk, darknessPropagationQueue, lightPropagationQueue, channel));

            uint lightValue = LightUtils.GetChannelColor(lightSource, channel);
            LightUtils.SetLightOfChannel(chunk, sourceChunkLocalPos, channel, lightValue);
            lightPropagationQueue.Enqueue(new LightAddNode(chunk, sourceChunkLocalPos));

            updatedChunks.UnionWith(PropagateLight(world, chunk, lightPropagationQueue, channel));

            darknessPropagationQueue.Clear();
            lightPropagationQueue.Clear();
        }

        updatedChunks.Remove(chunk);
        return updatedChunks.ToArray();
    }

    private static void ClearChannelAt(
        Chunk chunk,
        Vector3i chunkLocalPos,
        LightChannel channel,
        Queue<LightRemoveNode> darknessPropagationQueue)
    {
        uint currentLightValue = LightUtils.GetLightOfChannel(chunk, chunkLocalPos, channel);
        darknessPropagationQueue.Enqueue(new LightRemoveNode(chunk, chunkLocalPos, currentLightValue));
        LightUtils.SetLightOfChannel(chunk, chunkLocalPos, channel, 0);
    }

    private static HashSet<Chunk> PropagateDarkness(
        World world,
        Chunk chunk,
        Queue<LightRemoveNode> darkQueue,
        Queue<LightAddNode> lightQueue,
        LightChannel channel)
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

                uint neighbourLight = LightUtils.GetLightOfChannel(currentChunk, position, channel);

                if (neighbourLight > 0 && neighbourLight < lightRemoveNode.LightValue)
                {
                    LightUtils.SetLightOfChannel(currentChunk, position, channel, 0);
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

    private static HashSet<Chunk> PropagateLight(
        World world,
        Chunk chunk,
        Queue<LightAddNode> lightQueue,
        LightChannel channel)
    {
        HashSet<Chunk> processedChunks = new();

        while (lightQueue.Count != 0)
        {
            LightAddNode lightAddNode = lightQueue.Dequeue();

            uint currentLight = LightUtils.GetLightOfChannel(lightAddNode.Chunk, lightAddNode.ChunkLocalPos, channel);
            if (currentLight <= 1)
            {
                continue;
            }

            Vector3i[] neighbourPositions = lightAddNode.ChunkLocalPos.GetSurroundingPositions();
            for (int i = 0; i < neighbourPositions.Length; i++)
            {
                (Vector3i position, Chunk currentChunk) = BlockPropagation.FixReference(world, neighbourPositions[i],
                    lightAddNode.Chunk, out bool referenceFixable);

                if (!referenceFixable || currentChunk.GetBlockAt(position).GetBlock().IsOpaque)
                {
                    continue;
                }

                uint neighbourLight = LightUtils.GetLightOfChannel(currentChunk, position, channel);
                if (neighbourLight < currentLight - 1)
                {
                    LightUtils.SetLightOfChannel(currentChunk, position, channel, currentLight - 1);
                    lightQueue.Enqueue(new LightAddNode(currentChunk, position));

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
