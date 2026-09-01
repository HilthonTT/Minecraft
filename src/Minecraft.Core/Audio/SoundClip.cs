namespace Minecraft.Core.Audio;

public sealed class SoundClip
{
    public SoundClip(float[] samples)
    {
        Samples = samples;
    }

    public float[] Samples { get; }

    public TimeSpan Duration => TimeSpan.FromSeconds((double)Samples.Length / AudioEngine.SampleRate);
}
