using Minecraft.Core.Worlds.Chunks;

namespace Minecraft.Core.Worlds.Decoration.Trees;

public interface ITreeGenerator
{
    void GenerateTreeAt(Chunk chunk, int localX, int worldY, int localZ, Random random);
}
