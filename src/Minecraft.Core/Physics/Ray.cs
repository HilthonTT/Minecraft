using Minecraft.Core.Worlds.Blocks;
using OpenTK.Mathematics;
using Minecraft.Core.Worlds;
using Minecraft.Core.Utilities.Vectors;

namespace Minecraft.Core.Physics;

public struct Ray
{
    public Vector3 Origin { get; private set; }
    public Vector3 Direction { get; private set; }
    public Vector3 DirectionFrac { get; private set; }
    public float DistanceToIntersection { get; private set; }

    public Ray(Vector3 origin, Vector3 direction)
    {
        Origin = origin;
        Direction = direction.Normalized();
        DirectionFrac = new Vector3(1 / Direction.X, 1 / Direction.Y, 1 / Direction.Z);
        DistanceToIntersection = float.MaxValue;
    }

    public RayTraceResult? TraceWorld(World world, int maxDist = 1, int stepsPerBlock = 50)
    {
        Vector3 position = Origin;
        int maxSteps = maxDist * stepsPerBlock;
        Vector3 offset = Direction / stepsPerBlock;

        BlockState? hitBlockState = null;
        var blockPos = Vector3i.Zero;

        for (int i = 0; i < maxSteps; i++)
        {
            position += offset;

            var steppedPos = position.ToBlockPos();
            BlockState? state = world.GetBlockAt(steppedPos);
            if (state is not null && state.GetBlock() != BlockRegistry.Air)
            {
                hitBlockState = state;
                blockPos = steppedPos;
                break;
            }
        }

        if (hitBlockState is null)
        {
            return null;
        }

        AxisAlignedBox[] hitAABBs = hitBlockState.GetBlock().GetSelectionBox(hitBlockState, blockPos);
        AxisAlignedBox? hitAABB = null;
        float dist = float.MaxValue;
        foreach (AxisAlignedBox aabb in hitAABBs)
        {
            float hitDist = aabb.Intersects(this);
            if (hitDist < dist)
            {
                dist = hitDist;
                hitAABB = aabb;
            }
        }

        if (dist == float.MaxValue || hitAABB is null)
        {
            return null;
        }

        DistanceToIntersection = dist;
        Vector3 exactIntersection = Origin + Direction * DistanceToIntersection;
        Vector3 normalAtIntersection = hitAABB.GetNormalAtIntersectionPoint(exactIntersection);
        return new RayTraceResult(normalAtIntersection, exactIntersection, hitBlockState, blockPos);
    }
}
