using Minecraft.Core.Shapes;
using Minecraft.Core.Utilities.Vectors;
using Minecraft.Core.Worlds;
using Minecraft.Core.Worlds.Chunks;
using Minecraft.Core.Worlds.Lighting;
using OpenTK.Mathematics;

namespace Minecraft.Core.Render.MeshGenerator;

/// <summary>
/// Builds the vertex buffers for a chunk. The buffers are allocated once and reused for every chunk, since
/// meshing happens continuously and the arrays are far too large to churn through the heap.
/// </summary>
public abstract class MeshGenerator
{
    /// <summary>
    /// Upper bound on the vertex data a single chunk can produce. A chunk that exceeded this would have to
    /// be almost entirely made of plants, which each contribute far more geometry than a solid block.
    /// </summary>
    private const int BufferCapacity = 1048576;

    protected readonly BlockModelRegistry _blockModelRegistry;

    protected float[] _vertexPositions = new float[BufferCapacity];
    protected int _positionPointer;
    protected float[] _vertexUVs = new float[BufferCapacity];
    protected int _uvsPointer;
    protected uint[] _vertexLights = new uint[BufferCapacity];
    protected int _lightsPointer;
    protected float[] _vertexNormals = new float[BufferCapacity];
    protected int _normalPointer;
    protected int _indicesCount;

    protected MeshGenerator(BlockModelRegistry blockModelRegistry)
    {
        _blockModelRegistry = blockModelRegistry;
    }

    public ChunkBufferLayout GenerateMeshFor(World world, Chunk chunk)
    {
        ChunkBufferLayout chunkModel = GenerateMesh(world, chunk);
        ClearData();
        return chunkModel;
    }

    protected abstract ChunkBufferLayout GenerateMesh(World world, Chunk chunk);

    protected void ClearData()
    {
        _positionPointer = 0;
        _uvsPointer = 0;
        _lightsPointer = 0;
        _normalPointer = 0;
        _indicesCount = 0;
    }

    private void AddVector3(Vector3 vector)
    {
        _vertexPositions[_positionPointer++] = vector.X;
        _vertexPositions[_positionPointer++] = vector.Y;
        _vertexPositions[_positionPointer++] = vector.Z;
    }

    private void AddVector2(Vector2 vector)
    {
        _vertexUVs[_uvsPointer++] = vector.X;
        _vertexUVs[_uvsPointer++] = vector.Y;
    }

    protected void AddFacesToMeshFromFront(BlockFace[] toAddFaces, Vector3i blockPos, Light[] lights, bool flip)
    {
        foreach (BlockFace face in toAddFaces)
        {
            AddFace(face, blockPos, lights, flip, 0, 1, 2, 0, 2, 3);
        }
    }

    protected void AddFacesToMeshFromBack(BlockFace[] toAddFaces, Vector3i blockPos, Light[] lights, bool flip)
    {
        foreach (BlockFace face in toAddFaces)
        {
            AddFace(face, blockPos, lights, flip, 1, 0, 3, 1, 3, 2);
        }
    }

    protected void AddFacesToMeshDualSided(BlockFace[] toAddFaces, Vector3i blockPos, Light[] lights, bool flip)
    {
        AddFacesToMeshFromFront(toAddFaces, blockPos, lights, flip);
        AddFacesToMeshFromBack(toAddFaces, blockPos, lights, flip);
    }

    /// <summary>
    /// Emits one quad as two triangles. A quad has no single correct diagonal: splitting it the wrong way
    /// across an ambient occlusion gradient produces a visible seam, so the caller can ask for the other
    /// diagonal through <paramref name="flip"/>.
    /// </summary>
    private void AddFace(
        BlockFace face,
        Vector3i blockPos,
        Light[] lights,
        bool flip,
        int v1, int v2, int v3, int v4, int v5, int v6)
    {
        if (face.Positions.Length != lights.Length)
        {
            throw new ArgumentException(
                $"A face with {face.Positions.Length} vertices was given {lights.Length} light values.",
                nameof(lights));
        }

        Span<int> order = flip ? [v1, v2, v6, v2, v3, v6] : [v1, v2, v3, v4, v5, v6];

        foreach (int index in order)
        {
            AddVector2(face.TextureCoords[index]);
        }

        foreach (int index in order)
        {
            AddVector3(face.Positions[index].Plus(blockPos));
        }

        foreach (int index in order)
        {
            _vertexLights[_lightsPointer++] = lights[index].GetStorage();
        }

        for (int i = 0; i < 6; i++)
        {
            _vertexNormals[_normalPointer++] = face.Normal.X;
            _vertexNormals[_normalPointer++] = face.Normal.Y;
            _vertexNormals[_normalPointer++] = face.Normal.Z;
        }

        _indicesCount += 6;
    }
}
