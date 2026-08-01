using Minecraft.Core.Physics;
using Minecraft.Core.Worlds.Blocks.States;
using OpenTK.Mathematics;

namespace Minecraft.Core.Worlds.Blocks.Types;

public sealed class BlockAir : Block
{
    public BlockAir(ushort id) : base(id)
    {
        IsOpaque = false;
        IsOverridable = true;
    }

    public override BlockState GetNewDefaultState()
    {
        return new BlockStateAir();
    }

    public override AxisAlignedBox[] GetCollisionBox(BlockState state, Vector3i blockPos)
    {
        return _emptyAABB;
    }

    public override AxisAlignedBox[] GetSelectionBox(BlockState state, Vector3i blockPos)
    {
        return _emptyAABB;
    }
}
