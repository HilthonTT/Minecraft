using Minecraft.Core.Worlds.Lighting;
using OpenTK.Mathematics;

namespace Minecraft.Core.Worlds.Blocks.States;

/// <summary>
/// Glowstone, which grows in the deep caverns and is the only light the world generates with. Its colour is
/// fixed rather than drawn per block the way TNT's is, so every vein lights its cave the same warm white.
/// </summary>
public sealed class BlockStateGlowstone : BlockState, ILightSource
{
    public Vector3i LightColor { get; } = new(14, 12, 8);

    public override Block GetBlock()
    {
        return BlockRegistry.Glowstone;
    }
}
