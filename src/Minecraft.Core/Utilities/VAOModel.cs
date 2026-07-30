using Minecraft.Core.Render;
using Minecraft.Core.World.Lighting;
using OpenTK.Graphics.OpenGL;
using System.Runtime.InteropServices;

namespace Minecraft.Core.Utilities;

public sealed class VAOModel
{
    private int _vaoId;
    private readonly List<int> _buffers = [];

    public int IndicesCount { get; private set; }

    public VAOModel(
        float[] positions, 
        float[] textureCoordinates, 
        float[] lights, 
        float[] normals, 
        int indicesCount)
    {
        IndicesCount = indicesCount;
        CreateVAO();
        BindVAO();
        CreateVBO(3, positions);
        CreateVBO(3, normals);
        CreateVBO(2, textureCoordinates);
        CreateVBO(1, lights);
        UnbindVAO();
    }

    public VAOModel(ChunkBufferLayout chunkLayout)
    {
        IndicesCount = chunkLayout.IndicesCount;
        CreateVAO();
        BindVAO();
        CreateVBO(3, chunkLayout.VertexPositions, chunkLayout.PositionsPointer);
        CreateVBO(3, chunkLayout.VertexNormals, chunkLayout.NormalsPointer);
        CreateVBO(2, chunkLayout.VertexUVs, chunkLayout.UVsPointer);
        CreateVBO(1, chunkLayout.VertexLights, chunkLayout.LightsPointer);
        UnbindVAO();
    }

    public VAOModel(float[] positions, int[] indices)
    {
        IndicesCount = indices.Length;
        CreateVAO();
        BindVAO();
        CreateVBO(3, positions);
        CreateIBO(indices);
        UnbindVAO();
    }

    public VAOModel(float[] positions, float[] textureCoordinates, int indicesCount)
    {
        IndicesCount = indicesCount;
        CreateVAO();
        BindVAO();
        CreateVBO(3, positions);
        CreateVBO(2, textureCoordinates);
        UnbindVAO();
    }

    public void CleanUp()
    {
        foreach (int buffer in _buffers)
        {
            GL.DeleteBuffer(buffer);
        }
        GL.DeleteVertexArray(_vaoId);
        _buffers.Clear();
    }


    public void BindVAO()
    {
        GL.BindVertexArray(_vaoId);
    }

    public static void UnbindVAO()
    {
        GL.BindVertexArray(0);
    }

    private void CreateVAO()
    {
        _vaoId = GL.GenVertexArray();
    }

    private static int SizeOf<T>()
        where T : struct
    {
        return Marshal.SizeOf(default(T));
    }

    /// <summary> Creates an index buffer object and buffers the given indices. </summary>
    private void CreateIBO(int[] indices)
    {
        int vboID = GL.GenBuffer();
        _buffers.Add(vboID);
        GL.BindBuffer(BufferTarget.ElementArrayBuffer, vboID);
        GL.BufferData(BufferTarget.ElementArrayBuffer, (IntPtr)(indices.Length * sizeof(int)), indices, BufferUsageHint.StaticDraw);
    }

    /// <summary>
    /// Creates a vertex bufffer object and buffers the given float values. The integer specifies the number of elements in the datastructure.
    /// A Vector3 would for example have this integer set to 3 (X, Y, Z)
    /// </summary>
    private void CreateVBO<T>(int nrOfElementsInStructure, T[] data, int overrideLength = -1) 
        where T : struct
    {
        VertexAttribPointerType dataType = VertexAttribPointerType.Float;
        if (typeof(T) == typeof(Light) || typeof(T) == typeof(uint))
        {
            //TODO This should be changed later to actually support multiple attribute types.
            dataType = VertexAttribPointerType.Float;
        }

        int vboID = GL.GenBuffer();
        GL.BindBuffer(BufferTarget.ArrayBuffer, vboID);

        if (overrideLength == -1)
        {
            GL.BufferData(BufferTarget.ArrayBuffer, (IntPtr)(data.Length * SizeOf<T>()), data, BufferUsageHint.StaticDraw);
        }
        else
        {
            GL.BufferData(BufferTarget.ArrayBuffer, (IntPtr)(overrideLength * SizeOf<T>()), data, BufferUsageHint.StaticDraw);
        }

        GL.VertexAttribPointer(_buffers.Count, nrOfElementsInStructure, dataType, false, 0, 0);
        GL.BindBuffer(BufferTarget.ArrayBuffer, 0);
        GL.EnableVertexAttribArray(_buffers.Count);
        _buffers.Add(vboID);
    }
}
