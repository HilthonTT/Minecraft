using Minecraft.Core.Utilities.Spatial;

namespace Minecraft.Core.Worlds.Blocks;

public interface IFacingBlockState
{
    Direction Facing { get; set; }
}
