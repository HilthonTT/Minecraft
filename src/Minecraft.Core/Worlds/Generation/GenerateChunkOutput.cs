using Minecraft.Core.Worlds.Chunks;
using OpenTK.Mathematics;

namespace Minecraft.Core.Worlds.Generation;

public sealed class GenerateChunkOutput
{
    private int _undecidedRecipients;
    private bool _adopted;

    public GenerateChunkOutput(Chunk chunk, World world, Vector2 gridPosition, int recipients)
    {
        Chunk = chunk;
        World = world;
        GridPosition = gridPosition;
        _undecidedRecipients = recipients;
    }

    public Chunk Chunk { get; }

    public World World { get; }

    public Vector2 GridPosition { get; }

    public void Adopt()
    {
        Volatile.Write(ref _adopted, true);
        Interlocked.Decrement(ref _undecidedRecipients);
    }

    public void Discard()
    {
        if (Interlocked.Decrement(ref _undecidedRecipients) == 0 && !Volatile.Read(ref _adopted))
        {
            World.ChunkPool.ReturnObject(Chunk);
        }
    }
}
