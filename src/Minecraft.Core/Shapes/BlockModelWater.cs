using Minecraft.Core.Textures;
using OpenTK.Mathematics;

namespace Minecraft.Core.Shapes;

/// <summary>
/// The surface of a body of water, built to the depth the water stands at in its cell. A source sits just
/// short of the top of its cell, running water lower the thinner it gets, and water on its way down a drop
/// fills the cell outright so a fall reads as a solid column.
/// <para>
/// Every side reports itself as see through, since a block sitting in the water still has to draw the face
/// it shares with it — hiding that face would leave a hole looking straight into the terrain.
/// </para>
/// </summary>
/// <remarks>
/// Nothing here stops the water drawing the faces it shares with the water beside it, which would put a grid
/// of surfaces through the inside of every sea. That is the mesh generator's job rather than the model's,
/// because it is the only side of the pair that knows what the block being meshed actually is, and so the
/// only one that can tell water lying against deeper water from water lying against shallower.
/// </remarks>
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

        // The bottom of the cell is left where it is and everything above the waterline is cut away, so that
        // water still meets the ground it lies on exactly.
        float top = surfaceHeight;

        _topFace = [new(0, top, 0), new(0, top, 1), new(1, top, 1), new(1, top, 0)];
        _backFace = [new(1, 0, 0), new(0, 0, 0), new(0, top, 0), new(1, top, 0)];
        _rightFace = [new(1, 0, 1), new(1, 0, 0), new(1, top, 0), new(1, top, 1)];
        _frontFace = [new(0, 0, 1), new(1, 0, 1), new(1, top, 1), new(0, top, 1)];
        _leftFace = [new(0, 0, 0), new(0, 0, 1), new(0, top, 1), new(0, top, 0)];

        // Cropped to match rather than left spanning the whole cell, otherwise a shortened side squashes a
        // full block of texture into it and shallow water ripples at a different scale from deep water.
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
