using OpenTK.Mathematics;

namespace Minecraft.Core.Worlds.Blocks;

public interface IOrientedBlockState
{
    void OrientTowardsSupport(Vector3i offsetToSupport);
}
