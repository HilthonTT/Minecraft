using OpenTK.Mathematics;

namespace Minecraft.Core.Shapes;

public readonly record struct SkinBox(
    Vector2i TexOffset,
    Vector3i TexSize,
    Vector3 Origin,
    SkinBoxPose Pose = SkinBoxPose.Upright,
    float Inflate = 0)
{
    public Vector3 Size => (Pose == SkinBoxPose.Upright
        ? new Vector3(TexSize.X, TexSize.Y, TexSize.Z)
        : new Vector3(TexSize.X, TexSize.Z, TexSize.Y)) + new Vector3(Inflate * 2.0F);
}
