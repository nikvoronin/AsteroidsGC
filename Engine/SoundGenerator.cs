using System.IO;

namespace AsteroidsWpf.Engine;

/// <summary>
/// Synthesizes short PCM WAV clips in memory so the game needs no shipped audio assets.
/// </summary>
public static class SoundGenerator
{
    private const int SampleRate = 44100;

    public static MemoryStream GenerateBeep(double frequency, double durationSeconds, double amplitude = 0.5)
    {
        int sampleCount = (int)(SampleRate * durationSeconds);
        var samples = new short[sampleCount];
        for (int i = 0; i < sampleCount; i++)
        {
            double t = i / (double)SampleRate;
            double envelope = 1.0 - t / durationSeconds;
            double value = Math.Sign(Math.Sin(2 * Math.PI * frequency * t)) * amplitude * envelope;
            samples[i] = (short)(value * short.MaxValue);
        }
        return WriteWav(samples);
    }

    public static MemoryStream GenerateNoiseBurst(double durationSeconds, double amplitude, double lowPassCutoff)
    {
        int sampleCount = (int)(SampleRate * durationSeconds);
        var samples = new short[sampleCount];
        var rng = Random.Shared;
        double filtered = 0;
        double alpha = lowPassCutoff / (lowPassCutoff + SampleRate / (2 * Math.PI));
        for (int i = 0; i < sampleCount; i++)
        {
            double t = i / (double)SampleRate;
            double envelope = 1.0 - t / durationSeconds;
            double raw = rng.NextDouble() * 2 - 1;
            filtered += alpha * (raw - filtered);
            samples[i] = (short)(filtered * amplitude * envelope * short.MaxValue);
        }
        return WriteWav(samples);
    }

    public static MemoryStream GenerateLoopingRumble(double durationSeconds, double frequency, double amplitude)
    {
        int sampleCount = (int)(SampleRate * durationSeconds);
        var samples = new short[sampleCount];
        var rng = Random.Shared;
        for (int i = 0; i < sampleCount; i++)
        {
            double t = i / (double)SampleRate;
            double tone = Math.Sin(2 * Math.PI * frequency * t);
            double noise = (rng.NextDouble() * 2 - 1) * 0.3;
            samples[i] = (short)((tone * 0.7 + noise) * amplitude * short.MaxValue);
        }
        return WriteWav(samples);
    }

    public static MemoryStream GenerateWarble(double freqA, double freqB, double warbleRate, double durationSeconds, double amplitude)
    {
        int sampleCount = (int)(SampleRate * durationSeconds);
        var samples = new short[sampleCount];
        for (int i = 0; i < sampleCount; i++)
        {
            double t = i / (double)SampleRate;
            double mix = 0.5 + 0.5 * Math.Sin(2 * Math.PI * warbleRate * t);
            double frequency = freqA + (freqB - freqA) * mix;
            double value = Math.Sign(Math.Sin(2 * Math.PI * frequency * t)) * amplitude;
            samples[i] = (short)(value * short.MaxValue);
        }
        return WriteWav(samples);
    }

    public static MemoryStream GenerateArpeggio(double[] frequencies, double noteDurationSeconds, double amplitude)
    {
        int totalSamples = (int)(SampleRate * noteDurationSeconds * frequencies.Length);
        var samples = new short[totalSamples];
        int samplesPerNote = (int)(SampleRate * noteDurationSeconds);
        for (int n = 0; n < frequencies.Length; n++)
        {
            double frequency = frequencies[n];
            for (int i = 0; i < samplesPerNote; i++)
            {
                double t = i / (double)SampleRate;
                double envelope = 1.0 - t / noteDurationSeconds;
                double value = Math.Sign(Math.Sin(2 * Math.PI * frequency * t)) * amplitude * envelope;
                samples[n * samplesPerNote + i] = (short)(value * short.MaxValue);
            }
        }
        return WriteWav(samples);
    }

    private static MemoryStream WriteWav(short[] samples)
    {
        var stream = new MemoryStream();
        using (var writer = new BinaryWriter(stream, System.Text.Encoding.ASCII, leaveOpen: true))
        {
            int byteRate = SampleRate * 2;
            int dataSize = samples.Length * 2;

            writer.Write("RIFF"u8.ToArray());
            writer.Write(36 + dataSize);
            writer.Write("WAVE"u8.ToArray());

            writer.Write("fmt "u8.ToArray());
            writer.Write(16);
            writer.Write((short)1); // PCM
            writer.Write((short)1); // mono
            writer.Write(SampleRate);
            writer.Write(byteRate);
            writer.Write((short)2); // block align
            writer.Write((short)16); // bits per sample

            writer.Write("data"u8.ToArray());
            writer.Write(dataSize);
            foreach (short sample in samples)
            {
                writer.Write(sample);
            }
        }

        stream.Position = 0;
        return stream;
    }
}
