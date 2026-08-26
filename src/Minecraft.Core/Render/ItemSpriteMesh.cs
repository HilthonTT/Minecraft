using Minecraft.Core.Textures;
using Minecraft.Core.Worlds.Lighting;
using OpenTK.Mathematics;

namespace Minecraft.Core.Render;

/// <summary>
/// Builds one cell of the item sheet as a mesh: a square standing upright, wearing that cell on both of its
/// faces.
/// <para>
/// Where a block is drawn as the block itself, turned onto a corner so that its shape is what identifies it,
/// an item has no shape to turn — a pickaxe is a picture of a pickaxe, and a pickaxe seen edge on would be a
/// line. So it is drawn flat and given a second face pointing the other way, which is what keeps one lying on
/// the ground visible all the way round as it turns.
/// </para>
/// <para>
/// The cut out is the artwork's own: the sheet carries a real alpha channel and the fragment shader already
/// throws away anything under half of it, which is the same test the see through cells of the block sheet go
/// through. Nothing here has to know the silhouette.
/// </para>
/// </summary>
public static class ItemSpriteMesh
{
    /// <summary>
    /// Lit flat and from no direction in particular. There is no lit side and shaded side of a flat square,
    /// and shading its two faces differently would make an item lying on the ground flicker as it turned.
    /// </summary>
    private const uint Brightness = 56;

    public static VAOModel Build(TextureAtlas itemAtlas, Vector2 iconCell)
    {
        Vector2[] uvs = itemAtlas.GetTextureCoords(iconCell);

        // Built around its own middle, so that turning it spins it on the spot the way a block icon does.
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
