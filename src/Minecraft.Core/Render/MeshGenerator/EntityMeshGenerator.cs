using Minecraft.Core.Shapes;
using Minecraft.Core.Utilities;
using OpenTK.Mathematics;

namespace Minecraft.Core.Render.MeshGenerator;

/// <summary>
/// Builds the mesh for an entity model. Entities are lit uniformly rather than from the world lightmap, so
/// this is a straight conversion from faces to vertex buffers.
/// </summary>
public sealed class EntityMeshGenerator
{
    private readonly List<float> _vertexPositions = [];
    private readonly List<float> _textureUVs = [];
    private readonly List<float> _illuminations = [];
    private readonly List<float> _normals = [];
    private int _indicesCount;

    /// <summary>
    /// Which corners of a four sided face make up its two triangles, wound the same way round as the corners
    /// themselves. Faces are emitted as triangles rather than as quads because quads were taken out of the
    /// core profile, and a driver that holds to that draws nothing at all when handed one.
    /// </summary>
    private static readonly int[] _triangleCorners = [0, 1, 2, 0, 2, 3];

    public VAOModel GenerateMeshFor(EntityModel entityModel)
    {
        foreach (BlockFace face in entityModel.EntityFaces)
        {
            foreach (int corner in _triangleCorners)
            {
                Vector2 uv = face.TextureCoords[corner];
                _textureUVs.Add(uv.X);
                _textureUVs.Add(uv.Y);

                Vector3 modelSpacePosition = face.Positions[corner];
                _vertexPositions.Add(modelSpacePosition.X);
                _vertexPositions.Add(modelSpacePosition.Y);
                _vertexPositions.Add(modelSpacePosition.Z);

                _illuminations.Add(1.0F);
                _normals.Add(face.Normal.X);
                _normals.Add(face.Normal.Y);
                _normals.Add(face.Normal.Z);
            }

            _indicesCount += _triangleCorners.Length;
        }

        var model = new VAOModel(
            [.. _vertexPositions],
            [.. _textureUVs],
            [.. _illuminations],
            [.. _normals],
            _indicesCount);

        ClearData();
        return model;
    }

    private void ClearData()
    {
        _vertexPositions.Clear();
        _textureUVs.Clear();
        _illuminations.Clear();
        _normals.Clear();
        _indicesCount = 0;
    }
}
