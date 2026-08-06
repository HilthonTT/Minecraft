using Minecraft.Core.Shapes;
using Minecraft.Core.Utilities.Vectors;
using Minecraft.Core.Worlds;
using Minecraft.Core.Worlds.Chunks;
using Minecraft.Core.Worlds.Lighting;
using OpenTK.Mathematics;

namespace Minecraft.Core.Render.MeshGenerator;

/// <summary>
/// Builds the vertex buffers for a chunk. A chunk produces two meshes rather than one, since its water has
/// to be drawn after everything else, and both are filled in a single walk over the blocks.
/// </summary>
public abstract class MeshGenerator
{
    /// <summary>
    /// Upper bound on the solid vertex data a single chunk can produce. A chunk that exceeded this would
    /// have to be almost entirely made of plants, which each contribute far more geometry than a solid
    /// block.
    /// </summary>
    private const int OpaqueBufferCapacity = 1048576;

    /// <summary>
    /// The same for the water. Far smaller, because the inside of a body of water carries no geometry at
    /// all: only the faces where it meets air are drawn, which for a sea is its surface and little else.
    /// </summary>
    private const int LiquidBufferCapacity = 262144;

    protected readonly BlockModelRegistry _blockModelRegistry;

    private readonly MeshBuffers _opaqueBuffers = new(OpaqueBufferCapacity);
    private readonly MeshBuffers _liquidBuffers = new(LiquidBufferCapacity);

    /// <summary>Whichever of the two the block being meshed right now belongs in.</summary>
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

    /// <summary>Directs everything emitted from here on into one buffer set or the other.</summary>
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
