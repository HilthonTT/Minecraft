namespace Minecraft.Core.Audio;

/// <summary>
/// One decoded sound, held in memory as mono samples at the engine's own rate.
/// <para>
/// Kept mono however it was recorded. A sound in the world is placed by the engine, so a clip that carried
/// its own stereo image would fight with where the thing making it actually is, and mono halves what the
/// whole set costs to hold.
/// </para>
/// </summary>
public sealed class SoundClip
{
    public SoundClip(float[] samples)
    {
        Samples = samples;
    }

    public float[] Samples { get; }

    public TimeSpan Duration => TimeSpan.FromSeconds((double)Samples.Length / AudioEngine.SampleRate);
}
