using Minecraft.Core.Worlds;
using Minecraft.Core.Worlds.Chunks;
using OpenTK.Mathematics;

namespace Minecraft.Core.Worlds.Chunks;

public static class BlockPropagation
{
    /// <summary>
    /// Light propagates one block at a time, so a step can walk a chunk local position off the edge of the
    /// chunk it belongs to. This maps such a position back onto the neighbouring chunk that actually
    /// contains it.
    /// <para>
    /// <paramref name="wasReferenceFixable"/> is false when the neighbouring chunk is not loaded, or when
    /// the step left the build height, in which case the returned values must not be used.
    /// </para>
    /// </summary>
    public static (Vector3i Position, Chunk Chunk) FixReference(
        World world,
        Vector3i position,
        Chunk chunk,
        out bool wasReferenceFixable)
    {
        // Only ever one block outside, since propagation moves a single step at a time.
        if (position.X is < -1 or > 16 || position.Z is < -1 or > 16)
        {
            throw new ArgumentOutOfRangeException(
                nameof(position),
                position,
                "Propagation stepped more than one block outside of the chunk.");
        }

        wasReferenceFixable = position.Y >= 0 && position.Y < Constants.MAX_BUILD_HEIGHT;
        if (!wasReferenceFixable)
        {
            return (position, chunk);
        }

        int chunkOffsetX = position.X < 0 ? -1 : position.X > 15 ? 1 : 0;
        int chunkOffsetZ = position.Z < 0 ? -1 : position.Z > 15 ? 1 : 0;

        if (chunkOffsetX == 0 && chunkOffsetZ == 0)
        {
            return (position, chunk);
        }

        var neighbourPos = new Vector2(chunk.GridX + chunkOffsetX, chunk.GridZ + chunkOffsetZ);
        if (!world.LoadedChunks.TryGetValue(neighbourPos, out Chunk? neighbour))
        {
            wasReferenceFixable = false;
            return (position, chunk);
        }

        // Wrapping into [0, 15] turns -1 into 15 and 16 into 0, which is exactly the crossing over.
        position.X &= 15;
        position.Z &= 15;
        return (position, neighbour);
    }
}
