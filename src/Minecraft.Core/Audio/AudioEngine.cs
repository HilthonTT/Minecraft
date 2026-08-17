using Minecraft.Core.Logging;
using NAudio.Wave;
using NAudio.Wave.SampleProviders;
using OpenTK.Mathematics;

namespace Minecraft.Core.Audio;

/// <summary>
/// Plays sounds, and places the ones that happen somewhere in the world relative to where the camera is
/// listening from.
/// <para>
/// Everything goes through one output device and one mixer. A sound is added to it as a voice of its own and
/// removed again when it has been read to the end, so nothing has to be allocated per playing sound beyond
/// the voice itself, and the clips behind them are shared rather than copied.
/// </para>
/// </summary>
public sealed class AudioEngine : IDisposable
{
    /// <summary>The rate everything is decoded to, which is what nearly the whole sound set is recorded at.</summary>
    public const int SampleRate = 44100;

    /// <summary>Stereo, so that a sound can be placed left or right of where the camera is looking.</summary>
    public static readonly WaveFormat MixerFormat = WaveFormat.CreateIeeeFloatWaveFormat(SampleRate, 2);

    /// <summary>
    /// How far away a sound can be heard from. Past it nothing is queued at all, which is what keeps a
    /// distant hillside of sheep from filling the mixer with voices too quiet to make out.
    /// </summary>
    private const float MaxAudibleDistance = 34F;

    /// <summary>
    /// Below this the pan is wound back towards the middle. A sound the listener is standing almost on top
    /// of has no meaningful direction, and letting it swing hard to one ear as they turn is unpleasant.
    /// </summary>
    private const float FullPanDistance = 3F;

    /// <summary>
    /// A ceiling on how many sounds play at once. Reached only by something like a blast taking a hillside
    /// apart; past it a new sound is dropped rather than queued, since what is already playing covers it.
    /// </summary>
    private const int MaxConcurrentVoices = 48;

    private readonly MixingSampleProvider? _mixer;
    private readonly WaveOut? _output;

    /// <summary>
    /// How many voices are playing. Counted here rather than read off the mixer, whose own list is handed
    /// out unguarded and is being added to and removed from by the audio thread while the game thread would
    /// be walking it.
    /// </summary>
    private int _activeVoices;

    private Vector3 _listenerPosition;
    private Vector3 _listenerRight = Vector3.UnitX;

    /// <summary>Whether there is an output to play through at all, which a machine without one has not.</summary>
    public bool IsAvailable => _output is not null;

    /// <summary>
    /// What every sound is scaled by on its way out, from silent to as recorded. Applied when a voice is
    /// created rather than to the mixer, so changing it leaves whatever is already playing alone and takes
    /// effect on the next sound instead of jumping the volume of a clip midway through.
    /// </summary>
    public float MasterVolume { get; set; } = 1.0F;

    public AudioEngine()
    {
        try
        {
            // ReadFully keeps the mixer handing back silence when nothing is playing, rather than reporting
            // that it has run dry and stopping the device that would have to be restarted for the next sound.
            _mixer = new MixingSampleProvider(MixerFormat) { ReadFully = true };
            _mixer.MixerInputEnded += (_, _) => Interlocked.Decrement(ref _activeVoices);

            _output = new WaveOut();
            _output.Init(new SampleToWaveProvider(_mixer));
            _output.Play();
        }
        catch (Exception exception)
        {
            // A machine with no sound device is still one the game should run on, so this is reported and
            // then dropped: every call below turns into nothing.
            Logger.Warn($"No audio output is available, the game will run without sound: {exception.Message}");
            _mixer = null;
            _output = null;
        }
    }

    /// <summary>
    /// Tells the engine where sounds are being heard from. Taken from the active camera rather than the
    /// player, so the detached overhead camera hears the world from where it is actually looking at it.
    /// </summary>
    public void UpdateListener(Vector3 position, Vector3 right)
    {
        _listenerPosition = position;
        _listenerRight = right;
    }

    /// <summary>Plays a sound at full volume in both ears, for something that has no place in the world.</summary>
    public void Play(SoundClip? clip, float volume = 1F, float pitch = 1F)
    {
        AddVoice(clip, volume, volume, pitch);
    }

    /// <summary>
    /// Plays a sound as coming from a place in the world: quieter the further off it is, and weighted
    /// towards the ear it is on.
    /// </summary>
    public void PlayAt(SoundClip? clip, Vector3 position, float volume = 1F, float pitch = 1F)
    {
        if (clip is null || _mixer is null)
        {
            return;
        }

        Vector3 toSource = position - _listenerPosition;
        float distance = toSource.Length;
        if (distance >= MaxAudibleDistance)
        {
            return;
        }

        // Squared rather than straight, so a sound fades off the way distance actually thins one out instead
        // of holding most of its volume until it abruptly runs out.
        float falloff = 1F - (distance / MaxAudibleDistance);
        float attenuated = volume * falloff * falloff;

        float pan = 0F;
        if (distance > 0.001F)
        {
            pan = Vector3.Dot(toSource / distance, _listenerRight);

            // Wound back to the middle for anything close enough that its direction does not read.
            pan *= Math.Clamp(distance / FullPanDistance, 0F, 1F);
        }

        // Constant power, so a sound keeps its loudness as it crosses from one ear to the other rather than
        // dipping in the middle the way a straight split would.
        float angle = (pan + 1F) * 0.25F * MathF.PI;
        AddVoice(clip, attenuated * MathF.Cos(angle), attenuated * MathF.Sin(angle), pitch);
    }

    private void AddVoice(SoundClip? clip, float leftGain, float rightGain, float pitch)
    {
        if (clip is null || _mixer is null)
        {
            return;
        }

        leftGain *= MasterVolume;
        rightGain *= MasterVolume;

        // Nothing worth hearing, and every voice costs a slot whether or not it can be made out. Tested after
        // the master volume, so a game turned all the way down queues no voices at all.
        if (leftGain <= 0.001F && rightGain <= 0.001F)
        {
            return;
        }

        if (Volatile.Read(ref _activeVoices) >= MaxConcurrentVoices)
        {
            return;
        }

        var voice = new SoundVoice(clip, leftGain, rightGain, Math.Clamp(pitch, 0.25F, 4F));

        // Counted before the voice is handed over, since the audio thread can finish and count it back down
        // the moment it has been.
        Interlocked.Increment(ref _activeVoices);
        _mixer.AddMixerInput(voice);
    }

    public void StopAll()
    {
        if (_mixer is null)
        {
            return;
        }

        // Clearing them does not raise the ended event they would each have counted themselves down with.
        _mixer.RemoveAllMixerInputs();
        Interlocked.Exchange(ref _activeVoices, 0);
    }

    public void Dispose()
    {
        StopAll();
        _output?.Dispose();
    }
}
