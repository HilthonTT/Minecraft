using Minecraft.Core.Utilities.Vectors;
using Minecraft.Core.Worlds;
using Minecraft.Core.Worlds.Blocks;
using Minecraft.Core.Worlds.Blocks.Types;
using OpenTK.Mathematics;

namespace Minecraft.Core.Render;

public readonly struct FogState
{
    private static readonly Vector3 UnderwaterTint = new(0.02F, 0.16F, 0.32F);

    private static readonly Vector3 LavaTint = new(0.62F, 0.18F, 0.02F);

    private const float LavaFogStart = 0.1F;
    private const float LavaFogEnd = 2.5F;

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

        Block? liquid = LiquidAt(world, cameraPosition);

        if (liquid is BlockLava)
        {
            return new FogState
            {
                Color = LavaTint,
                Start = LavaFogStart,
                End = LavaFogEnd,
                CameraSubmerged = true,
            };
        }

        if (liquid is null)
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

    private static Block? LiquidAt(World world, Vector3 position)
    {
        var blockPos = position.ToBlockPos();
        if (world.IsOutsideBuildHeight(blockPos.Y))
        {
            return null;
        }

        Block block = world.GetBlockAt(blockPos).GetBlock();
        return block.IsLiquid ? block : null;
    }
}
