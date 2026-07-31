using Minecraft.Core.World.Blocks;
using Vector3i = Minecraft.Core.Utilities.Vector.Vector3i;

namespace Minecraft.Core.World;

public sealed class World
{
    public BlockState? GetBlockAt(Vector3i blockPos)
    {
        throw new NotImplementedException();
    }
}
