namespace Minecraft.Core.Render.MeshGenerator;

/// <summary>
/// One set of vertex arrays being filled. The arrays are allocated once and reused for every chunk, since
/// meshing happens continuously and they are far too large to churn through the heap.
/// </summary>
public sealed class MeshBuffers
{
    public readonly float[] Positions;
    public readonly float[] UVs;
    public readonly uint[] Lights;
    public readonly float[] Normals;

    public int PositionsPointer;
    public int UVsPointer;
    public int LightsPointer;
    public int NormalsPointer;
    public int IndicesCount;

    public MeshBuffers(int capacity)
    {
        Positions = new float[capacity];
        UVs = new float[capacity];
        Lights = new uint[capacity];
        Normals = new float[capacity];
    }

    public void Clear()
    {
        PositionsPointer = 0;
        UVsPointer = 0;
        LightsPointer = 0;
        NormalsPointer = 0;
        IndicesCount = 0;
    }

    public ChunkBufferLayout ToLayout()
    {
        return new ChunkBufferLayout
        {
            VertexPositions = Positions,
            PositionsPointer = PositionsPointer,
            VertexUVs = UVs,
            UVsPointer = UVsPointer,
            VertexLights = Lights,
            LightsPointer = LightsPointer,
            VertexNormals = Normals,
            NormalsPointer = NormalsPointer,
            IndicesCount = IndicesCount,
        };
    }
}
