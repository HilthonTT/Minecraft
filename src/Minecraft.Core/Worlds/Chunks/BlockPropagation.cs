using Minecraft.Core.Worlds;
using Minecraft.Core.Worlds.Chunks;
using OpenTK.Mathematics;

namespace Minecraft.Core.Worlds.Chunks;

public static class BlockPropagation
{
    public static (Vector3i Position, Chunk Chunk) FixReference(
        World world,
        Vector3i position,
        Chunk chunk,
        out bool wasReferenceFixable)
    {
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

        position.X &= 15;
        position.Z &= 15;
        return (position, neighbour);
    }
}
