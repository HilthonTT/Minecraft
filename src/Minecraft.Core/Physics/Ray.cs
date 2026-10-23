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

        Vector3i? lastTested = null;

        for (int i = 0; i < maxSteps; i++)
        {
            position += offset;

            var blockPos = position.ToBlockPos();
            if (blockPos == lastTested)
            {
                continue;
            }

            lastTested = blockPos;

            BlockState? state = world.GetBlockAt(blockPos);
            if (state is null)
            {
                continue;
            }

            Block block = state.GetBlock();
            if (block == BlockRegistry.Air || block.IsLiquid)
            {
                continue;
            }

            RayTraceResult? hit = Intersect(state, blockPos, block.GetSelectionBox(state, blockPos));
            if (hit is not null)
            {
                return hit;
            }

            // The ray crossed this cell without touching what is in it — the empty half above a slab, the air
            // beside a fence post — so whatever is behind it is still in view.
        }

        return null;
    }

    private RayTraceResult? Intersect(BlockState state, Vector3i blockPos, AxisAlignedBox[] boxes)
    {
        AxisAlignedBox? hitAABB = null;
        float dist = float.MaxValue;
        foreach (AxisAlignedBox aabb in boxes)
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
        return new RayTraceResult(normalAtIntersection, exactIntersection, state, blockPos);
    }
}
