using Minecraft.Core.Textures;
using Minecraft.Core.Utilities;
using Minecraft.Core.Worlds.Blocks;
using OpenTK.Mathematics;

namespace Minecraft.Core.Shapes;

/// <summary>
/// A model that fills its whole cell. Every face is hidden by an opaque neighbour, so nothing is emitted
/// unconditionally.
/// </summary>
public abstract class FullBlockModel : BlockModel
{
    // Counter clockwise starting bottom-left-back when facing the face from the front.
    protected Vector3[] _backFace = [new(1, 0, 0), new(0, 0, 0), new(0, 1, 0), new(1, 1, 0)];
    protected Vector3[] _rightFace = [new(1, 0, 1), new(1, 0, 0), new(1, 1, 0), new(1, 1, 1)];
    protected Vector3[] _frontFace = [new(0, 0, 1), new(1, 0, 1), new(1, 1, 1), new(0, 1, 1)];
    protected Vector3[] _leftFace = [new(0, 0, 0), new(0, 0, 1), new(0, 1, 1), new(0, 1, 0)];
    protected Vector3[] _topFace = [new(0, 1, 0), new(0, 1, 1), new(1, 1, 1), new(1, 1, 0)];
    protected Vector3[] _bottomFace = [new(1, 0, 0), new(1, 0, 1), new(0, 0, 1), new(0, 0, 0)];

    protected Vector2[] _uvBack = [], _uvRight = [], _uvFront = [], _uvLeft = [], _uvTop = [], _uvBottom = [];

    /// <summary>Reused so that meshing a chunk does not allocate an array per face.</summary>
    private readonly BlockFace[] _partialFaces = new BlockFace[1];

    protected FullBlockModel(TextureAtlas textureAtlas) : base(textureAtlas)
    {
        SetStandardUVs();

        _back = true;
        _right = true;
        _front = true;
        _left = true;
        _top = true;
        _bottom = true;
    }

    public override BlockFace[] GetAlwaysVisibleFaces(BlockState state, Vector3i blockPos)
    {
        return _emptyArray;
    }

    public override BlockFace[] GetPartialVisibleFaces(BlockState state, Vector3i blockPos, Direction direction)
    {
        _partialFaces[0] = direction switch
        {
            Direction.Back => new BlockFace(_backFace, _uvBack),
            Direction.Right => new BlockFace(_rightFace, _uvRight),
            Direction.Front => new BlockFace(_frontFace, _uvFront),
            Direction.Left => new BlockFace(_leftFace, _uvLeft),
            Direction.Top => new BlockFace(_topFace, _uvTop),
            Direction.Bottom => new BlockFace(_bottomFace, _uvBottom),
            _ => throw new ArgumentOutOfRangeException(nameof(direction)),
        };

        return _partialFaces;
    }

    protected abstract void SetStandardUVs();

    /// <summary>Assigns the same atlas cell to all six sides.</summary>
    protected void SetUniformUVs(Vector2 atlasCell)
    {
        _uvBack = _textureAtlas.GetTextureCoords(atlasCell);
        _uvRight = _textureAtlas.GetTextureCoords(atlasCell);
        _uvFront = _textureAtlas.GetTextureCoords(atlasCell);
        _uvLeft = _textureAtlas.GetTextureCoords(atlasCell);
        _uvTop = _textureAtlas.GetTextureCoords(atlasCell);
        _uvBottom = _textureAtlas.GetTextureCoords(atlasCell);
    }

    /// <summary>Assigns one atlas cell to the four sides and separate cells to the top and bottom.</summary>
    protected void SetUVs(Vector2 sideCell, Vector2 topCell, Vector2 bottomCell)
    {
        _uvBack = _textureAtlas.GetTextureCoords(sideCell);
        _uvRight = _textureAtlas.GetTextureCoords(sideCell);
        _uvFront = _textureAtlas.GetTextureCoords(sideCell);
        _uvLeft = _textureAtlas.GetTextureCoords(sideCell);
        _uvTop = _textureAtlas.GetTextureCoords(topCell);
        _uvBottom = _textureAtlas.GetTextureCoords(bottomCell);
    }
}
