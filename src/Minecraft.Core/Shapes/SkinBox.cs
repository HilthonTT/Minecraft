using OpenTK.Mathematics;

namespace Minecraft.Core.Shapes;

/// <summary>
/// One cuboid of a mob, cut from the box net of a skin sheet in the layout Minecraft skins use.
/// </summary>
/// <param name="TexOffset">
/// The top left corner of the box's net on the sheet, in texels. The net lays the six faces out as two rows:
/// the bottom and top caps sit above the ring of side faces.
/// </param>
/// <param name="TexSize">The width, height and depth of the box as the net was drawn, in texels.</param>
/// <param name="Origin">
/// The corner the box grows from, in model units. X and Z are measured out from the model's vertical axis and
/// Y up from its feet.
/// </param>
/// <param name="Pose">Which way up the box is placed, and with it which face of the net faces where.</param>
public readonly record struct SkinBox(
    Vector2i TexOffset,
    Vector3i TexSize,
    Vector3 Origin,
    SkinBoxPose Pose = SkinBoxPose.Upright)
{
    /// <summary>
    /// The size of the box in model space, which is the size of its net with the axes swapped to match how
    /// it is posed.
    /// </summary>
    public Vector3 Size => Pose == SkinBoxPose.Upright
        ? new Vector3(TexSize.X, TexSize.Y, TexSize.Z)
        : new Vector3(TexSize.X, TexSize.Z, TexSize.Y);
}
