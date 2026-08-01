using Minecraft.Core.Textures;
using Minecraft.Core.Utilities;
using Minecraft.Core.Worlds.Blocks;
using OpenTK.Mathematics;

namespace Minecraft.Core.Shapes;

/// <summary>
/// A model made of two quads crossing each other diagonally, the usual shape for plants. It covers no side
/// of its cell, so its faces are always emitted and always double sided.
/// </summary>
public abstract class ScissorModel : BlockModel
{
    protected Vector3[] _bladeOneFace = [new(1, 0, 1), new(0, 0, 0), new(0, 1, 0), new(1, 1, 1)];
    protected Vector3[] _bladeTwoFace = [new(1, 0, 0), new(0, 0, 1), new(0, 1, 1), new(1, 1, 0)];

    protected Vector2[] _uvBladeOne = [], _uvBladeTwo = [];

    protected ScissorModel(TextureAtlas textureAtlas) : base(textureAtlas)
    {
        SetStandardUVs();
        DoubleSidedFaces = true;
    }

    public override BlockFace[] GetAlwaysVisibleFaces(BlockState state, Vector3i blockPos)
    {
        return
        [
            new BlockFace(_bladeOneFace, _uvBladeOne),
            new BlockFace(_bladeTwoFace, _uvBladeTwo),
        ];
    }

    public override BlockFace[] GetPartialVisibleFaces(BlockState state, Vector3i blockPos, Direction direction)
    {
        return _emptyArray;
    }

    protected abstract void SetStandardUVs();

    protected void SetBladeUVs(Vector2 atlasCell)
    {
        _uvBladeOne = _textureAtlas.GetTextureCoords(atlasCell);
        _uvBladeTwo = _textureAtlas.GetTextureCoords(atlasCell);
    }
}
