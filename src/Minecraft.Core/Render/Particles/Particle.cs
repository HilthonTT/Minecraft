using OpenTK.Mathematics;

namespace Minecraft.Core.Render.Particles;

/// <summary>
/// One flying speck. Deliberately a struct in a flat array: there are hundreds of these at a time and every
/// one of them is walked over each frame, so allocating a class per speck would cost more than simulating it.
/// </summary>
public struct Particle
{
    public Vector3 Position;
    public Vector3 Velocity;

    /// <summary>Counts down to zero, at which point the slot is free again.</summary>
    public float RemainingSeconds;

    /// <summary>What it started with, so how far through its life it is can be worked out.</summary>
    public float TotalSeconds;

    /// <summary>How wide the speck is drawn, in blocks.</summary>
    public float Size;

    /// <summary>The patch of the texture sheet this speck is a piece of.</summary>
    public Vector2 UVMin;
    public Vector2 UVMax;

    /// <summary>Downwards pull, in blocks per second squared. Zero for anything that drifts rather than falls.</summary>
    public float Gravity;

    /// <summary>The share of its speed a speck loses each second to the air it is moving through.</summary>
    public float Drag;

    /// <summary>Whether the speck is stopped by the world, or passes through it.</summary>
    public bool CollidesWithWorld;

    /// <summary>The light where it was thrown from, packed the same way a vertex of the world carries it.</summary>
    public uint PackedLight;

    /// <summary>Whether this slot holds a speck that is still alive.</summary>
    public readonly bool IsAlive => RemainingSeconds > 0F;
}
