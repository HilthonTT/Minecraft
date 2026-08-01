using Minecraft.Core.Textures;
using OpenTK.Mathematics;

namespace Minecraft.Core.Shapes;

/// <summary>A plain half by two by half box, used to stand in for any entity that has no model of its own.</summary>
public sealed class DummyEntityModel : EntityModel
{
    public DummyEntityModel(TextureAtlas textureAtlas) : base(textureAtlas)
    {
        const float width = 0.5F;
        const float height = 2.0F;

        Vector3[] backFace = [new(width, 0, 0), new(0, 0, 0), new(0, height, 0), new(width, height, 0)];
        Vector3[] rightFace = [new(width, 0, width), new(width, 0, 0), new(width, height, 0), new(width, height, width)];
        Vector3[] frontFace = [new(0, 0, width), new(width, 0, width), new(width, height, width), new(0, height, width)];
        Vector3[] leftFace = [new(0, 0, 0), new(0, 0, width), new(0, height, width), new(0, height, 0)];
        Vector3[] topFace = [new(0, height, width), new(width, height, width), new(width, height, 0), new(0, height, 0)];
        Vector3[] bottomFace = [new(0, 0, 0), new(width, 0, 0), new(width, 0, width), new(0, 0, width)];

        Vector2[] uv = textureAtlas.GetTextureCoords(new Vector2(2, 12));

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
