namespace Minecraft.Core.Audio;

public sealed class SoundGroup
{
    private readonly SoundClip[] _variants;

    public SoundGroup(SoundClip[] variants)
    {
        _variants = variants;
    }

    public bool IsEmpty => _variants.Length == 0;

    public SoundClip? Pick(Random random)
    {
        return _variants.Length == 0 ? null : _variants[random.Next(_variants.Length)];
    }
}
