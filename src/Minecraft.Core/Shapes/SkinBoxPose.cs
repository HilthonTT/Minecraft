namespace Minecraft.Core.Shapes;

/// <summary>How a box is turned before it is placed, which decides where each face of its net ends up.</summary>
public enum SkinBoxPose
{
    /// <summary>The net's height axis runs up the world, the usual case.</summary>
    Upright,

    /// <summary>
    /// The box is tipped a quarter turn onto its back, so the net's height axis runs front to back instead.
    /// What was the back of the net becomes the top and what was the front becomes the underside, which is
    /// how an animal's long body is unwrapped: as though it were reared up on its hind legs, with its belly
    /// towards the viewer.
    /// </summary>
    Lying,
}
