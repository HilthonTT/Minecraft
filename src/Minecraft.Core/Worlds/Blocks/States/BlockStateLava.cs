using Minecraft.Core.Worlds.Lighting;
using OpenTK.Mathematics;

namespace Minecraft.Core.Worlds.Blocks.States;

public sealed class BlockStateLava(Block block) : BlockState, ILightSource
{
    public Vector3i LightColor { get; } = new(15, 10, 4);

    public override Block GetBlock() => block;

    public override string ToString() => block.GetType().Name + "[" + block.Id + "]";
}
