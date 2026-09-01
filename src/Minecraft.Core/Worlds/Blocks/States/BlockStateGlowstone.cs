using Minecraft.Core.Worlds.Lighting;
using OpenTK.Mathematics;

namespace Minecraft.Core.Worlds.Blocks.States;

public sealed class BlockStateGlowstone : BlockState, ILightSource
{
    public Vector3i LightColor { get; } = new(14, 12, 8);

    public override Block GetBlock()
    {
        return BlockRegistry.Glowstone;
    }
}
