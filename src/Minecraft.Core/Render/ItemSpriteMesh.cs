using Minecraft.Core.Textures;
using Minecraft.Core.Worlds.Lighting;
using OpenTK.Mathematics;

namespace Minecraft.Core.Render;

public static class ItemSpriteMesh
{
    private const uint Brightness = 56;

    public static VAOModel Build(TextureAtlas itemAtlas, Vector2 iconCell)
    {
        Vector2[] uvs = itemAtlas.GetTextureCoords(iconCell);

        Vector3[] corners =
        [
            new(0.5F, -0.5F, 0F),
            new(-0.5F, -0.5F, 0F),
            new(-0.5F, 0.5F, 0F),
            new(0.5F, 0.5F, 0F),
        ];

        const int vertexCount = 12;

        var layout = new ChunkBufferLayout
        {
            VertexPositions = new float[vertexCount * 3],
            VertexNormals = new float[vertexCount * 3],
            VertexUVs = new float[vertexCount * 2],
            VertexLights = new uint[vertexCount],
        };

        var light = new Light(0, 0, 0, 15 * BlockIconMesh.LightScale, 0);
        light.SetBrightness(Brightness);
        uint packedLight = light.GetStorage();

        AddQuad(ref layout, corners, uvs, packedLight, flipWinding: false);
        AddQuad(ref layout, corners, uvs, packedLight, flipWinding: true);

        layout.IndicesCount = vertexCount;
        return new VAOModel(layout);
    }

    private static void AddQuad(
        ref ChunkBufferLayout layout,
        Vector3[] corners,
        Vector2[] uvs,
        uint packedLight,
        bool flipWinding)
    {
        ReadOnlySpan<int> order = flipWinding ? [1, 0, 3, 1, 3, 2] : [0, 1, 2, 0, 2, 3];
        Vector3 normal = flipWinding ? -Vector3.UnitZ : Vector3.UnitZ;

        foreach (int index in order)
        {
            layout.VertexPositions[layout.PositionsPointer++] = corners[index].X;
            layout.VertexPositions[layout.PositionsPointer++] = corners[index].Y;
            layout.VertexPositions[layout.PositionsPointer++] = corners[index].Z;

            layout.VertexNormals[layout.NormalsPointer++] = normal.X;
            layout.VertexNormals[layout.NormalsPointer++] = normal.Y;
            layout.VertexNormals[layout.NormalsPointer++] = normal.Z;

            layout.VertexUVs[layout.UVsPointer++] = uvs[index].X;
            layout.VertexUVs[layout.UVsPointer++] = uvs[index].Y;

            layout.VertexLights[layout.LightsPointer++] = packedLight;
        }
    }
}
