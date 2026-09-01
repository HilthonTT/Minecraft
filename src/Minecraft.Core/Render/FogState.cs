using Minecraft.Core.Utilities.Vectors;
using Minecraft.Core.Worlds;
using OpenTK.Mathematics;

namespace Minecraft.Core.Render;

public readonly struct FogState
{
    private static readonly Vector3 UnderwaterTint = new(0.02F, 0.16F, 0.32F);

    private const float UnderwaterFogStart = 0.5F;
    private const float UnderwaterFogEnd = 22F;

    private const float UnderwaterDaylightScale = 1.6F;
    private const float UnderwaterDaylightMin = 0.08F;
    private const float UnderwaterDaylightMax = 1.0F;

    public Vector3 Color { get; private init; }

    public float Start { get; private init; }

    public float End { get; private init; }

    public bool CameraSubmerged { get; private init; }

    public static FogState ForCamera(World world, Vector3 cameraPosition, float startDistance, float endDistance)
    {
        Vector3 skyColor = world.Environment.GetCurrentFogColor();

        if (!IsPositionInLiquid(world, cameraPosition))
        {
            return new FogState
            {
                Color = skyColor,
                Start = startDistance,
                End = endDistance,
                CameraSubmerged = false,
            };
        }

        float daylight = (skyColor.X + skyColor.Y + skyColor.Z) / 3.0F;

        return new FogState
        {
            Color = UnderwaterTint * Math.Clamp(
                daylight * UnderwaterDaylightScale,
                UnderwaterDaylightMin,
                UnderwaterDaylightMax),
            Start = UnderwaterFogStart,
            End = UnderwaterFogEnd,
            CameraSubmerged = true,
        };
    }

    private static bool IsPositionInLiquid(World world, Vector3 position)
    {
        var blockPos = position.ToBlockPos();
        if (world.IsOutsideBuildHeight(blockPos.Y))
        {
            return false;
        }

        return world.GetBlockAt(blockPos).GetBlock().IsLiquid;
    }
}
