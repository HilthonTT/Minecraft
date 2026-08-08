using Minecraft.Core.Shapes;
using Minecraft.Core.Utilities.Spatial;
using Minecraft.Core.Utilities.Vectors;
using Minecraft.Core.Worlds.Blocks;
using Minecraft.Core.Worlds.Lighting;
using OpenTK.Mathematics;

namespace Minecraft.Core.Render;

/// <summary>
/// Builds a single block as a mesh of its own, standing in no world and buried against nothing.
/// <para>
/// Every face is taken, hidden ones included, since a block held in a hand or drawn in a slot has no
/// neighbours for any of them to be covered by, and the mesh is pulled back onto its own middle so that
/// turning it spins it on the spot rather than swinging it around the corner its model is built from.
/// </para>
/// </summary>
public static class BlockIconMesh
{
    // The same shading by orientation the chunk mesher uses, so a block off in a corner of the screen is lit
    // like one in the world rather than reading as a flat silhouette of itself.
    private const uint TopBrightness = 60;
    private const uint BottomBrightness = 36;
    private const uint SideXBrightness = 44;
    private const uint SideZBrightness = 52;

    /// <summary>Lightmap values are 0..15 while the packed channels are 0..63, so samples are scaled up.</summary>
    public const uint LightScale = 4;

    /// <summary>Open daylight, which is what a block drawn on the interface is lit by wherever the player is.</summary>
    public static Light FullDaylight => new(0, 0, 0, 15 * LightScale, 0);

    public static VAOModel Build(BlockModelRegistry blockModelRegistry, BlockState state, Light light)
    {
        BlockModel model = blockModelRegistry.Models[state.GetBlock().Id];
        List<BlockFace> faces = [];

        foreach (Direction direction in Enum.GetValues<Direction>())
        {
            faces.AddRange(model.GetPartialVisibleFaces(state, Vector3i.Zero, direction));
        }

        faces.AddRange(model.GetAlwaysVisibleFaces(state, Vector3i.Zero));

        int quadCount = faces.Count * (model.DoubleSidedFaces ? 2 : 1);
        int vertexCount = quadCount * 6;

        var layout = new ChunkBufferLayout
        {
            VertexPositions = new float[vertexCount * 3],
            VertexNormals = new float[vertexCount * 3],
            VertexUVs = new float[vertexCount * 2],
            VertexLights = new uint[vertexCount],
        };

        foreach (BlockFace face in faces)
        {
            AddQuad(ref layout, face, light, flipWinding: false);

            if (model.DoubleSidedFaces)
            {
                AddQuad(ref layout, face, light, flipWinding: true);
            }
        }

        layout.IndicesCount = vertexCount;
        return new VAOModel(layout);
    }

    private static void AddQuad(ref ChunkBufferLayout layout, BlockFace face, Light light, bool flipWinding)
    {
        ReadOnlySpan<int> order = flipWinding ? [1, 0, 3, 1, 3, 2] : [0, 1, 2, 0, 2, 3];

        Vector3 normal = flipWinding ? -face.Normal : face.Normal;

        light.SetBrightness(BrightnessFor(normal));
        uint packedLight = light.GetStorage();

        foreach (int index in order)
        {
            Vector3 position = face.Positions[index] - new Vector3(0.5F, 0.5F, 0.5F);

            layout.VertexPositions[layout.PositionsPointer++] = position.X;
            layout.VertexPositions[layout.PositionsPointer++] = position.Y;
            layout.VertexPositions[layout.PositionsPointer++] = position.Z;

            layout.VertexNormals[layout.NormalsPointer++] = normal.X;
            layout.VertexNormals[layout.NormalsPointer++] = normal.Y;
            layout.VertexNormals[layout.NormalsPointer++] = normal.Z;

            layout.VertexUVs[layout.UVsPointer++] = face.TextureCoords[index].X;
            layout.VertexUVs[layout.UVsPointer++] = face.TextureCoords[index].Y;

            layout.VertexLights[layout.LightsPointer++] = packedLight;
        }
    }

    /// <summary>Which of the fixed face shades an outward pointing normal falls under.</summary>
    private static uint BrightnessFor(Vector3 normal)
    {
        if (normal.LengthSquared < 0.0001F)
        {
            return SideZBrightness;
        }

        Vector3 unit = Vector3.Normalize(normal);

        if (unit.Y > 0.5F)
        {
            return TopBrightness;
        }

        if (unit.Y < -0.5F)
        {
            return BottomBrightness;
        }

        return MathF.Abs(unit.X) > MathF.Abs(unit.Z) ? SideXBrightness : SideZBrightness;
    }
}
