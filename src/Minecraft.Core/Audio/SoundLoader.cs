using Minecraft.Core.Logging;
using NVorbis;

namespace Minecraft.Core.Audio;

/// <summary>
/// Decodes Ogg Vorbis files into <see cref="SoundClip"/>s, normalising everything onto the one format the
/// mixer works in so that nothing has to be converted again while the game is running.
/// </summary>
public static class SoundLoader
{
    /// <summary>
    /// Decodes one file, or null when it could not be read. A missing or broken sound is not worth taking
    /// the game down for: it plays silently instead.
    /// </summary>
    public static SoundClip? TryLoad(string absolutePath)
    {
        try
        {
            using var reader = new VorbisReader(absolutePath);

            float[] interleaved = ReadAll(reader);
            float[] mono = ToMono(interleaved, reader.Channels);
            float[] resampled = Resample(mono, reader.SampleRate, AudioEngine.SampleRate);

            return new SoundClip(resampled);
        }
        catch (Exception exception)
        {
            Logger.Warn($"Could not load sound '{absolutePath}': {exception.Message}");
            return null;
        }
    }

    private static float[] ReadAll(VorbisReader reader)
    {
        // Sized from the length the file declares, so the common case fills it exactly and never grows.
        var samples = new List<float>((int)(reader.TotalTime.TotalSeconds * reader.SampleRate * reader.Channels) + 1024);
        var buffer = new float[reader.Channels * 4096];

        int read;
        while ((read = reader.ReadSamples(buffer, 0, buffer.Length)) > 0)
        {
            samples.AddRange(new ReadOnlySpan<float>(buffer, 0, read));
        }

        return [.. samples];
    }

    /// <summary>Averages the channels together, which for these files means folding a stereo pair into one.</summary>
    private static float[] ToMono(float[] interleaved, int channels)
    {
        if (channels == 1)
        {
            return interleaved;
        }

        var mono = new float[interleaved.Length / channels];
        for (int frame = 0; frame < mono.Length; frame++)
        {
            float sum = 0;
            for (int channel = 0; channel < channels; channel++)
            {
                sum += interleaved[(frame * channels) + channel];
            }

            mono[frame] = sum / channels;
        }

        return mono;
    }

    /// <summary>
    /// Puts a clip onto the engine's sample rate by reading it back at a fractional step and interpolating
    /// between neighbouring samples. Nearly every file is already at the right rate and returns untouched;
    /// this is for the handful that are not, which would otherwise play at the wrong speed and pitch.
    /// </summary>
    private static float[] Resample(float[] samples, int fromRate, int toRate)
    {
        if (fromRate == toRate || samples.Length == 0)
        {
            return samples;
        }

        double step = (double)fromRate / toRate;
        var resampled = new float[(int)(samples.Length / step)];

        for (int i = 0; i < resampled.Length; i++)
        {
            double source = i * step;
            int index = (int)source;
            float fraction = (float)(source - index);

            float current = samples[index];
            float next = index + 1 < samples.Length ? samples[index + 1] : current;
            resampled[i] = current + ((next - current) * fraction);
        }

        return resampled;
    }
}
