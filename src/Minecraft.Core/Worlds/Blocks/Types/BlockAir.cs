using Minecraft.Core.Inventories;
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
        SecondsToBreak = 0F;
    }

    public override BlockState GetNewDefaultState()
    {
        return new BlockStateAir();
    }

    public override ItemStack GetDrop(BlockState blockState) => ItemStack.Empty;

    public override AxisAlignedBox[] GetCollisionBox(BlockState state, Vector3i blockPos)
    {
        return _emptyAABB;
    }

    public override AxisAlignedBox[] GetSelectionBox(BlockState state, Vector3i blockPos)
    {
        return _emptyAABB;
    }
}
