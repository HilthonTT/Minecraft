using NAudio.Wave;

namespace Minecraft.Core.Audio;

public sealed class SoundVoice : ISampleProvider
{
    private readonly float[] _samples;
    private readonly float _leftGain;
    private readonly float _rightGain;
    private readonly double _step;

    private double _position;

    public SoundVoice(SoundClip clip, float leftGain, float rightGain, float pitch)
    {
        _samples = clip.Samples;
        _leftGain = leftGain;
        _rightGain = rightGain;
        _step = pitch;
        WaveFormat = AudioEngine.MixerFormat;
    }

    public WaveFormat WaveFormat { get; }

    public bool IsFinished => _position >= _samples.Length - 1;

    public int Read(Span<float> buffer)
    {
        int written = 0;

        while (written + 2 <= buffer.Length)
        {
            if (IsFinished)
            {
                break;
            }

            int index = (int)_position;
            float fraction = (float)(_position - index);
            float sample = _samples[index] + ((_samples[index + 1] - _samples[index]) * fraction);

            buffer[written++] = sample * _leftGain;
            buffer[written++] = sample * _rightGain;

            _position += _step;
        }

        return written;
    }
}
