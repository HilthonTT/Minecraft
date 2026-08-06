using Minecraft.Core.Physics;
using Minecraft.Core.Worlds.Blocks.States;
using OpenTK.Mathematics;

namespace Minecraft.Core.Worlds.Blocks.Types;

/// <summary>
/// Standing water. It fills its cell but stops nothing: there is no collision box, so an entity swims
/// through it rather than standing on it, and no selection box, so a click aimed through it reaches the
/// ground underneath instead of hitting the water.
/// </summary>
/// <remarks>
/// The water does not flow. Every block of it is placed by the generator at or below sea level and stays
/// where it was put, so breaking the wall of a lake leaves the lake where it is rather than draining it.
/// </remarks>
public sealed class BlockWater : Block
{
    public BlockWater(ushort id) : base(id)
    {
        // Light passes through, so a shallow seabed stays lit rather than sitting in the dark under a lid.
        IsOpaque = false;

        // What lets a block be placed into water: the placement lands on the water cell and takes it over,
        // the same way it would an empty one.
        IsOverridable = true;

        IsLiquid = true;
    }

    public override BlockState GetNewDefaultState()
    {
        return new BlockStateWater();
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
