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
