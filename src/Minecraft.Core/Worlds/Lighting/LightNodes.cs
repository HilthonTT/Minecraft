using Minecraft.Core.Worlds.Chunks;
using OpenTK.Mathematics;

namespace Minecraft.Core.Worlds.Lighting;

internal readonly struct LightAddNode
{
    public readonly Chunk Chunk;
    public readonly Vector3i ChunkLocalPos;

    public LightAddNode(Chunk currentChunk, Vector3i currentChunkLocalPos)
    {
        Chunk = currentChunk;
        ChunkLocalPos = currentChunkLocalPos;
    }
}

internal readonly struct LightRemoveNode
{
    public readonly Chunk Chunk;
    public readonly Vector3i ChunkLocalPos;
    public readonly uint LightValue;

    public LightRemoveNode(Chunk currentChunk, Vector3i currentChunkLocalPos, uint currentLight)
    {
        Chunk = currentChunk;
        ChunkLocalPos = currentChunkLocalPos;
        LightValue = currentLight;
    }
}
