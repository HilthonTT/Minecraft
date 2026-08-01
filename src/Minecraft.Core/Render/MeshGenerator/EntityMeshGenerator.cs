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

    public VAOModel GenerateMeshFor(EntityModel entityModel)
    {
        foreach (BlockFace face in entityModel.EntityFaces)
        {
            foreach (Vector2 uv in face.TextureCoords)
            {
                _textureUVs.Add(uv.X);
                _textureUVs.Add(uv.Y);
            }

            foreach (Vector3 modelSpacePosition in face.Positions)
            {
                _vertexPositions.Add(modelSpacePosition.X);
                _vertexPositions.Add(modelSpacePosition.Y);
                _vertexPositions.Add(modelSpacePosition.Z);
            }

            for (int i = 0; i < face.Positions.Length; i++)
            {
                _illuminations.Add(1.0F);
                _normals.Add(face.Normal.X);
                _normals.Add(face.Normal.Y);
                _normals.Add(face.Normal.Z);
            }

            _indicesCount += face.Positions.Length;
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
