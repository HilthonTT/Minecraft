using OpenTK.Mathematics;

namespace Minecraft.Core.Shapes;

public struct BlockFace(Vector3[] positions, Vector2[] textureCoords)
{
    public Vector3[] Positions { get; private set; } = positions;

    public Vector2[] TextureCoords { get; private set; } = textureCoords;

    public Vector3 Normal { get; private set; } = Vector3.Cross(positions[1] - positions[0], positions[2] - positions[0]);
}
