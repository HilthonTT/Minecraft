using NAudio.Wave;

namespace Minecraft.Core.Audio;

/// <summary>
/// One sound being played, read once from start to finish and then dropped.
/// <para>
/// The clip it reads is mono; what comes out is the stereo pair the mixer wants, with the two sides weighted
/// to put the sound where it is in the world. Pitch is a matter of how fast the clip is stepped through,
/// which is also what shortens it, the way speeding up a recording does.
/// </para>
/// </summary>
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

    /// <summary>Whether the whole clip has been read, after which the mixer drops this voice.</summary>
    public bool IsFinished => _position >= _samples.Length - 1;

    public int Read(float[] buffer, int offset, int count)
    {
        int written = 0;

        // Two samples per frame, so a frame can only be written while there is room for both.
        while (written + 2 <= count)
        {
            if (IsFinished)
            {
                break;
            }

            // Read between samples rather than at one, so a pitch that is not a whole number of steps does
            // not land on the same sample twice and buzz.
            int index = (int)_position;
            float fraction = (float)(_position - index);
            float sample = _samples[index] + ((_samples[index + 1] - _samples[index]) * fraction);

            buffer[offset + written++] = sample * _leftGain;
            buffer[offset + written++] = sample * _rightGain;

            _position += _step;
        }

        // Returning nothing is what tells the mixer this voice is spent and can be let go of.
        return written;
    }
}
