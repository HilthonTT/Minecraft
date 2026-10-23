using Minecraft.Core.Textures;
using Minecraft.Core.Utilities.Spatial;
using Minecraft.Core.Worlds.Blocks;
using OpenTK.Mathematics;

namespace Minecraft.Core.Shapes;

public abstract class BlockModel
{
    protected readonly TextureAtlas _textureAtlas;
    protected readonly BlockFace[] _emptyArray = [];

    protected bool _back, _right, _front, _left, _top, _bottom;

    public bool DoubleSidedFaces { get; protected set; }

    /// <summary>
    /// Whether the always visible faces are shaded by which way they point, the way a full block's sides are.
    /// Blades of grass and torches are lit evenly from every side; a slab lit that way reads as a flat shape cut
    /// out of the screen.
    /// </summary>
    public bool ShadesFacesByDirection { get; protected set; }

    protected BlockModel(TextureAtlas textureAtlas)
    {
        _textureAtlas = textureAtlas;
    }

    public virtual bool IsOpaqueOnSide(Direction direction)
    {
        return direction switch
        {
            Direction.Back => _back,
            Direction.Right => _right,
            Direction.Front => _front,
            Direction.Left => _left,
            Direction.Top => _top,
            Direction.Bottom => _bottom,
            _ => throw new ArgumentOutOfRangeException(nameof(direction)),
        };
    }

    public abstract BlockFace[] GetAlwaysVisibleFaces(BlockState state, Vector3i blockPos);

    public abstract BlockFace[] GetPartialVisibleFaces(BlockState state, Vector3i blockPos, Direction direction);
}
