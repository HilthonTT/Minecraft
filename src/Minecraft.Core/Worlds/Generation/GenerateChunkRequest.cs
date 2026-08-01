using OpenTK.Mathematics;

namespace Minecraft.Core.Worlds.Generation;

public struct GenerateChunkRequest
{
    /// <summary>The id of the player who asked for this chunk.</summary>
    public int PlayerId;

    /// <summary>The chunk grid position the chunk should be generated at.</summary>
    public Vector2 GridPosition;

    /// <summary>The world the chunk should be generated in.</summary>
    public World World;

    public Action<GenerateChunkOutput> Callback;
}
