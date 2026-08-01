using Minecraft.Core.Worlds.Chunks;

namespace Minecraft.Core.Worlds.Generation;

public struct GenerateChunkOutput
{
    /// <summary>The generated chunk.</summary>
    public Chunk Chunk;

    /// <summary>The world the chunk was generated in.</summary>
    public World World;
}
