using OpenTK.Mathematics;

namespace Minecraft.Core.Worlds.Blocks;

/// <param name="BlockPos">Where the block is going.</param>
/// <param name="ClickedBlockPos">The block that was clicked to put it there.</param>
/// <param name="HitPoint">Where on the clicked block the click landed, in world coordinates.</param>
/// <param name="LookDirection">Which way the player was looking.</param>
public readonly record struct PlacementContext(
    World World,
    Vector3i BlockPos,
    Vector3i ClickedBlockPos,
    Vector3 HitPoint,
    Vector3 LookDirection)
{
    /// <summary>How far up its own cell the click landed, from 0 at the bottom to 1 at the top.</summary>
    public float HitHeightInCell => HitPoint.Y - MathF.Floor(HitPoint.Y);

    public bool ClickedUnderside => ClickedBlockPos.Y == BlockPos.Y + 1;

    public bool ClickedTopside => ClickedBlockPos.Y == BlockPos.Y - 1;
}

/// <summary>
/// A state that takes its shape from how it was put down — a slab from which half of the face was clicked, stairs
/// and doors from which way the player faced.
/// </summary>
public interface IPlacementState
{
    void ApplyPlacement(PlacementContext context);

    /// <summary>
    /// Copies only what placement decided. The server builds a fresh state and takes this much from the one the
    /// client sent, so that nothing else a client might have written into the payload reaches the world.
    /// </summary>
    void CopyPlacementFrom(BlockState other);
}
