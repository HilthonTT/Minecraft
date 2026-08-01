using Minecraft.Core.Worlds.Blocks;
using OpenTK.Mathematics;

namespace Minecraft.Core.Physics;

public sealed class RayTraceResult
{
    /// <summary> The normal of the intersection point with the block (what side it hit) </summary>
    public Vector3 NormalAtHit { get; private set; }
    /// <summary> The exact intersection point </summary>
    public Vector3 IntersectionPoint { get; private set; }
    /// <summary> The blockstate the ray hit </summary>
    public BlockState BlockstateHit { get; private set; }
    /// <summary> The position a block would be placed at if one were to be placed </summary>
    public Vector3i BlockPlacePosition { get; private set; }
    /// <summary> The grid position of the block the ray intersected </summary>
    public Vector3i IntersectedBlockPos { get; private set; }

    public RayTraceResult(Vector3 normalAtHit, Vector3 intersectedPoint, BlockState blockstateHit, Vector3i blockStatePos)
    {
        NormalAtHit = normalAtHit;
        IntersectionPoint = intersectedPoint;
        BlockstateHit = blockstateHit;
        IntersectedBlockPos = blockStatePos;
        // A new block goes against the face that was hit, not inside the block that was hit.
        BlockPlacePosition = blockStatePos + new Vector3i(
            (int)MathF.Round(normalAtHit.X),
            (int)MathF.Round(normalAtHit.Y),
            (int)MathF.Round(normalAtHit.Z));
    }
}
