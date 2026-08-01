using Minecraft.Core.Textures;
using Minecraft.Core.Utilities;
using Minecraft.Core.Worlds.Blocks;
using OpenTK.Mathematics;

namespace Minecraft.Core.Shapes;

/// <summary>
/// The geometry of a single block type. A model splits its geometry into faces that are only emitted when
/// the neighbouring block does not hide them, and faces that are always emitted regardless of neighbours.
/// </summary>
public abstract class BlockModel
{
    protected readonly TextureAtlas _textureAtlas;
    protected readonly BlockFace[] _emptyArray = [];

    protected bool _back, _right, _front, _left, _top, _bottom;

    /// <summary>
    /// Whether the always visible faces need emitting from both sides. True for models made of thin quads,
    /// which would otherwise disappear when looked at from behind.
    /// </summary>
    public bool DoubleSidedFaces { get; protected set; }

    protected BlockModel(TextureAtlas textureAtlas)
    {
        _textureAtlas = textureAtlas;
    }

    /// <summary>
    /// Whether this model completely covers the given side, in which case the neighbour on that side can
    /// skip emitting the face it shares with this block.
    /// </summary>
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
