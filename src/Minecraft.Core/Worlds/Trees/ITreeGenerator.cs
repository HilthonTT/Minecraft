using Minecraft.Core.Worlds.Chunks;

namespace Minecraft.Core.Worlds.Trees;

public interface ITreeGenerator
{
    void GenerateTreeAt(Chunk chunk, int worldX, int worldY, int worldZ);
}
