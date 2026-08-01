using Minecraft.Core.Worlds.Chunks;

namespace Minecraft.Core.Worlds.Decoration;

public interface IDecorator
{
    void Decorate(Chunk chunk, int worldY, int localX, int localZ);
}
