using Minecraft.Core.Logging;
using NAudio.Wave;
using NAudio.Wave.SampleProviders;
using OpenTK.Mathematics;

namespace Minecraft.Core.Audio;

public sealed class AudioEngine : IDisposable
{
    public const int SampleRate = 44100;

    public static readonly WaveFormat MixerFormat = WaveFormat.CreateIeeeFloatWaveFormat(SampleRate, 2);

    private const float MaxAudibleDistance = 34F;

    private const float FullPanDistance = 3F;

    private const int MaxConcurrentVoices = 48;

    private readonly MixingSampleProvider? _mixer;
    private readonly WaveOut? _output;

    private int _activeVoices;

    private Vector3 _listenerPosition;
    private Vector3 _listenerRight = Vector3.UnitX;

    public bool IsAvailable => _output is not null;

    public float MasterVolume { get; set; } = 1.0F;

    public AudioEngine()
    {
        try
        {
            _mixer = new MixingSampleProvider(MixerFormat) { ReadFully = true };
            _mixer.MixerInputEnded += (_, _) => Interlocked.Decrement(ref _activeVoices);

            _output = new WaveOut();
            _output.Init(new SampleToWaveProvider(_mixer));
            _output.Play();
        }
        catch (Exception exception)
        {
            Logger.Warn($"No audio output is available, the game will run without sound: {exception.Message}");
            _mixer = null;
            _output = null;
        }
    }

    public void UpdateListener(Vector3 position, Vector3 right)
    {
        _listenerPosition = position;
        _listenerRight = right;
    }

    public void Play(SoundClip? clip, float volume = 1F, float pitch = 1F)
    {
        AddVoice(clip, volume, volume, pitch);
    }

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

        float falloff = 1F - (distance / MaxAudibleDistance);
        float attenuated = volume * falloff * falloff;

        float pan = 0F;
        if (distance > 0.001F)
        {
            pan = Vector3.Dot(toSource / distance, _listenerRight);

            pan *= Math.Clamp(distance / FullPanDistance, 0F, 1F);
        }

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

        if (leftGain <= 0.001F && rightGain <= 0.001F)
        {
            return;
        }

        if (Volatile.Read(ref _activeVoices) >= MaxConcurrentVoices)
        {
            return;
        }

        var voice = new SoundVoice(clip, leftGain, rightGain, Math.Clamp(pitch, 0.25F, 4F));

        Interlocked.Increment(ref _activeVoices);
        _mixer.AddMixerInput(voice);
    }

    public void StopAll()
    {
        if (_mixer is null)
        {
            return;
        }

        _mixer.RemoveAllMixerInputs();
        Interlocked.Exchange(ref _activeVoices, 0);
    }

    public void Dispose()
    {
        StopAll();
        _output?.Dispose();
    }
}
