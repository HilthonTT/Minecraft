using Minecraft.Core.Utilities;
using OpenTK.Mathematics;

namespace Minecraft.Core.Render.Chunks;

public sealed class RenderChunk
{
    public RenderChunk(int gridPositionX, int gridPositionZ)
    {
        TransformationMatrix = MathUtils.CreateTransformationMatrix(new Vector3(gridPositionX * 16, 0, gridPositionZ * 16));
        GridPosition = new Vector2(gridPositionX, gridPositionZ);
    }

    public VAOModel? HardBlocksModel { get; set; }

    public Matrix4 TransformationMatrix { get; set; }

    public Vector2 GridPosition { get; set; }

    public void CleanUp()
    {
        HardBlocksModel?.CleanUp();
    }
}
