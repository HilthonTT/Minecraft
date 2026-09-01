using OpenTK.Mathematics;

namespace Minecraft.Core.Render.Particles;

public struct Particle
{
    public Vector3 Position;
    public Vector3 Velocity;

    public float RemainingSeconds;

    public float TotalSeconds;

    public float Size;

    public Vector2 UVMin;
    public Vector2 UVMax;

    public float Gravity;

    public float Drag;

    public bool CollidesWithWorld;

    public uint PackedLight;

    public readonly bool IsAlive => RemainingSeconds > 0F;
}
