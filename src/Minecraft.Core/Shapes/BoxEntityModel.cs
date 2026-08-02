using Minecraft.Core.Textures;
using OpenTK.Mathematics;

namespace Minecraft.Core.Shapes;

/// <summary>
/// A box wearing one cell of the block atlas on every face. Entities have no artwork of their own yet, so
/// every entity model is one of these, skinned with whichever block happens to look closest.
/// </summary>
public sealed class BoxEntityModel : EntityModel
{
    public BoxEntityModel(TextureAtlas textureAtlas, Vector2 atlasCell, float width, float height, float length)
        : base(textureAtlas)
    {
        Vector3[] backFace = [new(width, 0, 0), new(0, 0, 0), new(0, height, 0), new(width, height, 0)];
        Vector3[] rightFace = [new(width, 0, length), new(width, 0, 0), new(width, height, 0), new(width, height, length)];
        Vector3[] frontFace = [new(0, 0, length), new(width, 0, length), new(width, height, length), new(0, height, length)];
        Vector3[] leftFace = [new(0, 0, 0), new(0, 0, length), new(0, height, length), new(0, height, 0)];
        Vector3[] topFace = [new(0, height, length), new(width, height, length), new(width, height, 0), new(0, height, 0)];
        Vector3[] bottomFace = [new(0, 0, 0), new(width, 0, 0), new(width, 0, length), new(0, 0, length)];

        Vector2[] uv = textureAtlas.GetTextureCoords(atlasCell);

        EntityFaces =
        [
            new BlockFace(backFace, uv),
            new BlockFace(rightFace, uv),
            new BlockFace(frontFace, uv),
            new BlockFace(leftFace, uv),
            new BlockFace(topFace, uv),
            new BlockFace(bottomFace, uv),
        ];
    }
}
