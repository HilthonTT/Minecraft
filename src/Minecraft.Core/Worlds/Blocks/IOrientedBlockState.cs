using OpenTK.Mathematics;

namespace Minecraft.Core.Worlds.Blocks;

/// <summary>
/// A block state that cares which way round it was put down. The face that was clicked is known only at the
/// moment of placing, and is gone by the time the world has the block, so it is handed over here.
/// </summary>
public interface IOrientedBlockState
{
    /// <summary>
    /// Points the block at the block it was placed against.
    /// </summary>
    /// <param name="offsetToSupport">
    /// The step from where this block is going to the block that was clicked. Zero when the new block is
    /// replacing what was clicked rather than sitting beside it, in which case there is no face to read and
    /// the state should keep whatever it defaults to.
    /// </param>
    void OrientTowardsSupport(Vector3i offsetToSupport);
}
