namespace Minecraft.Core.Render.MeshGenerator;

public sealed class MeshBuffers
{
    public float[] Positions;
    public float[] UVs;
    public uint[] Lights;
    public float[] Normals;

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

    public void EnsureRoomForVertices(int vertexCount)
    {
        EnsureCapacity(ref Positions, PositionsPointer + (vertexCount * 3));
        EnsureCapacity(ref UVs, UVsPointer + (vertexCount * 2));
        EnsureCapacity(ref Lights, LightsPointer + vertexCount);
        EnsureCapacity(ref Normals, NormalsPointer + (vertexCount * 3));
    }

    private static void EnsureCapacity<T>(ref T[] buffer, int required)
    {
        if (required <= buffer.Length)
        {
            return;
        }

        int capacity = Math.Max(buffer.Length, 1);
        while (capacity < required)
        {
            capacity *= 2;
        }

        Array.Resize(ref buffer, capacity);
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
