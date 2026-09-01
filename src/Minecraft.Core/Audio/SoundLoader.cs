using Minecraft.Core.Logging;
using NVorbis;

namespace Minecraft.Core.Audio;

public static class SoundLoader
{
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
        var samples = new List<float>((int)(reader.TotalTime.TotalSeconds * reader.SampleRate * reader.Channels) + 1024);
        var buffer = new float[reader.Channels * 4096];

        int read;
        while ((read = reader.ReadSamples(buffer, 0, buffer.Length)) > 0)
        {
            samples.AddRange(new ReadOnlySpan<float>(buffer, 0, read));
        }

        return [.. samples];
    }

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
