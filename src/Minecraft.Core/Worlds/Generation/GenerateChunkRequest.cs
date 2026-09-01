using OpenTK.Mathematics;

namespace Minecraft.Core.Worlds.Generation;

public struct GenerateChunkRequest
{
    public int PlayerId;

    public Vector2 GridPosition;

    public World World;

    public Action<GenerateChunkOutput> Callback;
}
