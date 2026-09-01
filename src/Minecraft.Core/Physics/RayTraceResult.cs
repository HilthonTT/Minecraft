using Minecraft.Core.Worlds.Blocks;
using OpenTK.Mathematics;

namespace Minecraft.Core.Physics;

public sealed class RayTraceResult
{
    public Vector3 NormalAtHit { get; private set; }
    public Vector3 IntersectionPoint { get; private set; }
    public BlockState BlockstateHit { get; private set; }
    public Vector3i BlockPlacePosition { get; private set; }
    public Vector3i IntersectedBlockPos { get; private set; }

    public RayTraceResult(Vector3 normalAtHit, Vector3 intersectedPoint, BlockState blockstateHit, Vector3i blockStatePos)
    {
        NormalAtHit = normalAtHit;
        IntersectionPoint = intersectedPoint;
        BlockstateHit = blockstateHit;
        IntersectedBlockPos = blockStatePos;
        BlockPlacePosition = blockStatePos + new Vector3i(
            (int)MathF.Round(normalAtHit.X),
            (int)MathF.Round(normalAtHit.Y),
            (int)MathF.Round(normalAtHit.Z));
    }
}
