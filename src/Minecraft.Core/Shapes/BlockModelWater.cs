using Minecraft.Core.Textures;
using Minecraft.Core.Utilities;
using OpenTK.Mathematics;

namespace Minecraft.Core.Shapes;

/// <summary>
/// The surface of a body of water. Built as a full cube so the water meets the ground it lies on exactly,
/// but every side reports itself as see through, since a block sitting in the water still has to draw the
/// face it shares with it — hiding that face would leave a hole looking straight into the terrain.
/// </summary>
/// <remarks>
/// Nothing here stops the water drawing the faces it shares with the water beside it, which would put a
/// grid of surfaces through the inside of every sea. That is the mesh generator's job rather than the
/// model's, because it is the only side of the pair that knows what the block being meshed actually is.
/// </remarks>
public sealed class BlockModelWater : FullBlockModel
{
    public BlockModelWater(TextureAtlas textureAtlas) : base(textureAtlas)
    {
        _back = false;
        _right = false;
        _front = false;
        _left = false;
        _top = false;
        _bottom = false;
    }

    protected override void SetStandardUVs() => SetUniformUVs(BlockAtlas.Water);
}
