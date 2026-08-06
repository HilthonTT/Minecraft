namespace Minecraft.Core.Audio;

/// <summary>
/// The interchangeable recordings of one sound, of which a single one is picked each time it plays.
/// <para>
/// Nearly everything in the sound set is drawn several times over — six of grass being walked on, four of
/// stone being broken. Playing the same one every time is what makes a repeated sound read as a loop rather
/// than as the thing it is meant to be, so which one plays is drawn afresh on every use.
/// </para>
/// </summary>
public sealed class SoundGroup
{
    private readonly SoundClip[] _variants;

    public SoundGroup(SoundClip[] variants)
    {
        _variants = variants;
    }

    /// <summary>Whether this group has anything to play, which a group whose files were missing does not.</summary>
    public bool IsEmpty => _variants.Length == 0;

    public SoundClip? Pick(Random random)
    {
        return _variants.Length == 0 ? null : _variants[random.Next(_variants.Length)];
    }
}
