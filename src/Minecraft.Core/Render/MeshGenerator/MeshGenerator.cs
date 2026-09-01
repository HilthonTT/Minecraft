using Minecraft.Core.Shapes;
using Minecraft.Core.Utilities.Vectors;
using Minecraft.Core.Worlds;
using Minecraft.Core.Worlds.Chunks;
using Minecraft.Core.Worlds.Lighting;
using OpenTK.Mathematics;

namespace Minecraft.Core.Render.MeshGenerator;

public abstract class MeshGenerator
{
    private const int OpaqueBufferCapacity = 1048576;

    private const int LiquidBufferCapacity = 262144;

    protected readonly BlockModelRegistry _blockModelRegistry;

    private readonly MeshBuffers _opaqueBuffers = new(OpaqueBufferCapacity);
    private readonly MeshBuffers _liquidBuffers = new(LiquidBufferCapacity);

    private MeshBuffers _target;

    protected MeshGenerator(BlockModelRegistry blockModelRegistry)
    {
        _blockModelRegistry = blockModelRegistry;
        _target = _opaqueBuffers;
    }

    public ChunkMesh GenerateMeshFor(World world, Chunk chunk)
    {
        ChunkMesh mesh = GenerateMesh(world, chunk);
        ClearData();
        return mesh;
    }

    protected abstract ChunkMesh GenerateMesh(World world, Chunk chunk);

    protected void TargetLiquidBuffers(bool liquid)
    {
        _target = liquid ? _liquidBuffers : _opaqueBuffers;
    }

    protected ChunkMesh BuildChunkMesh()
    {
        return new ChunkMesh
        {
            Opaque = _opaqueBuffers.ToLayout(),
            Liquid = _liquidBuffers.ToLayout(),
        };
    }

    protected void ClearData()
    {
        _opaqueBuffers.Clear();
        _liquidBuffers.Clear();
        _target = _opaqueBuffers;
    }

    private void AddVector3(Vector3 vector)
    {
        _target.Positions[_target.PositionsPointer++] = vector.X;
        _target.Positions[_target.PositionsPointer++] = vector.Y;
        _target.Positions[_target.PositionsPointer++] = vector.Z;
    }

    private void AddVector2(Vector2 vector)
    {
        _target.UVs[_target.UVsPointer++] = vector.X;
        _target.UVs[_target.UVsPointer++] = vector.Y;
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
            _target.Lights[_target.LightsPointer++] = lights[index].GetStorage();
        }

        for (int i = 0; i < 6; i++)
        {
            _target.Normals[_target.NormalsPointer++] = face.Normal.X;
            _target.Normals[_target.NormalsPointer++] = face.Normal.Y;
            _target.Normals[_target.NormalsPointer++] = face.Normal.Z;
        }

        _target.IndicesCount += 6;
    }
}
