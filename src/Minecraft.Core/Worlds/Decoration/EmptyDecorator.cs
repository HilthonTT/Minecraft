using Minecraft.Core.Worlds.Chunks;

namespace Minecraft.Core.Worlds.Decoration;

public sealed class EmptyDecorator : IDecorator
{
    public void Decorate(Chunk chunk, int worldY, int localX, int localZ, Random random)
    {
    }
}
