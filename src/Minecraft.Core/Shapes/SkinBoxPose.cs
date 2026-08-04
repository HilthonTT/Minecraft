namespace Minecraft.Core.Shapes;

/// <summary>How a box is turned before it is placed, which decides where each face of its net ends up.</summary>
public enum SkinBoxPose
{
    /// <summary>The net's height axis runs up the world, the usual case.</summary>
    Upright,

    /// <summary>
    /// The box is tipped a quarter turn onto its front, so the net's height axis runs front to back instead.
    /// What was the front of the net becomes the top, which is how an animal's long body is unwrapped.
    /// </summary>
    Lying,
}
