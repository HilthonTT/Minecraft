using Minecraft.Core.Textures;
using OpenTK.Mathematics;

namespace Minecraft.Core.Shapes;

public sealed class BlockModelWater : FullBlockModel
{
    public BlockModelWater(TextureAtlas textureAtlas, float surfaceHeight) : base(textureAtlas)
    {
        _back = false;
        _right = false;
        _front = false;
        _left = false;
        _top = false;
        _bottom = false;

        if (surfaceHeight >= Constants.CUBE_DIM)
        {
            return;
        }

        float top = surfaceHeight;

        _topFace = [new(0, top, 0), new(0, top, 1), new(1, top, 1), new(1, top, 0)];
        _backFace = [new(1, 0, 0), new(0, 0, 0), new(0, top, 0), new(1, top, 0)];
        _rightFace = [new(1, 0, 1), new(1, 0, 0), new(1, top, 0), new(1, top, 1)];
        _frontFace = [new(0, 0, 1), new(1, 0, 1), new(1, top, 1), new(0, top, 1)];
        _leftFace = [new(0, 0, 0), new(0, 0, 1), new(0, top, 1), new(0, top, 0)];

        Vector2[] sideUVs = textureAtlas.GetTextureCoords(
            BlockAtlas.Water,
            new Vector2(0, Constants.CUBE_DIM - top),
            new Vector2(1, 1));

        _uvBack = sideUVs;
        _uvRight = sideUVs;
        _uvFront = sideUVs;
        _uvLeft = sideUVs;
    }

    protected override void SetStandardUVs() => SetUniformUVs(BlockAtlas.Water);
}
