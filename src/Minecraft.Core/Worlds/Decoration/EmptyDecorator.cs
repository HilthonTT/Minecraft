using Minecraft.Core.Worlds.Chunks;

namespace Minecraft.Core.Worlds.Decoration;

/// <summary>A decorator that leaves the terrain bare, so a biome never has to go without one.</summary>
public sealed class EmptyDecorator : IDecorator
{
    public void Decorate(Chunk chunk, int worldY, int localX, int localZ)
    {
    }
}
