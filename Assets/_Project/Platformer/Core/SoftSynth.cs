using System;

namespace SteepingSpirits.Platforming.Core
{
    /// <summary>
    /// Procedural placeholder sounds, written into sample buffers once at load
    /// time (never on the audio thread). Everything is soft: slow-ish attack,
    /// exponential decay, low-passed noise, peaks never above the given volume,
    /// silence at both ends (no clicks).
    /// </summary>
    public static class SoftSynth
    {
        public static int Samples(int sampleRate, float seconds) => Math.Max(1, (int)(sampleRate * seconds));

        /// <summary>Soft plucked tone (sine + quiet octave), e.g. jump.</summary>
        public static void Pluck(float[] buffer, int sampleRate, float frequency, float volume, float decaySeconds,
            float pitchGlide = 0f, int offset = 0)
        {
            double phase = 0;
            for (int i = offset; i < buffer.Length; i++)
            {
                float t = (i - offset) / (float)sampleRate;
                double f = frequency * (1.0 + pitchGlide * Math.Min(1.0, t / 0.12));
                phase += 2.0 * Math.PI * f / sampleRate;
                float wave = (float)(Math.Sin(phase) * 0.8 + Math.Sin(phase * 2.0) * 0.2);
                buffer[i] += wave * Envelope(t, buffer.Length - offset, sampleRate, 0.008f, decaySeconds) * volume;
            }
        }

        /// <summary>Low-passed noise puff, e.g. landing dust or a soft whoosh.</summary>
        public static void Puff(float[] buffer, int sampleRate, float volume, float cutoffHz, float decaySeconds, int seed)
        {
            var rng = new Random(seed);
            float a = OnePoleCoefficient(cutoffHz, sampleRate);
            float s1 = 0f, s2 = 0f;
            float peak = 1e-6f;
            var tmp = new float[buffer.Length];
            for (int i = 0; i < buffer.Length; i++)
            {
                float n = (float)(rng.NextDouble() * 2.0 - 1.0);
                s1 += a * (n - s1);
                s2 += a * (s1 - s2);
                float t = i / (float)sampleRate;
                tmp[i] = s2 * Envelope(t, buffer.Length, sampleRate, 0.012f, decaySeconds);
                peak = Math.Max(peak, Math.Abs(tmp[i]));
            }

            float gain = volume / peak;
            for (int i = 0; i < buffer.Length; i++)
            {
                buffer[i] += tmp[i] * gain;
            }
        }

        /// <summary>Gentle chime: notes start one after another and ring together.</summary>
        public static void Chime(float[] buffer, int sampleRate, float[] frequencies, float volume, float noteSpacing,
            float decaySeconds)
        {
            float each = volume / Math.Max(1, frequencies.Length);
            for (int n = 0; n < frequencies.Length; n++)
            {
                int offset = Math.Min(buffer.Length - 1, (int)(n * noteSpacing * sampleRate));
                Pluck(buffer, sampleRate, frequencies[n], each * 1.4f, decaySeconds, 0f, offset);
            }

            Normalize(buffer, volume);
        }

        /// <summary>Scales the buffer down so its peak is at most maxPeak.</summary>
        public static void Normalize(float[] buffer, float maxPeak)
        {
            float peak = 0f;
            foreach (float s in buffer) peak = Math.Max(peak, Math.Abs(s));
            if (peak <= maxPeak || peak <= 0f)
            {
                return;
            }

            float g = maxPeak / peak;
            for (int i = 0; i < buffer.Length; i++) buffer[i] *= g;
        }

        /// <summary>Attack ramp, exponential decay and a short fade to exact zero at the end.</summary>
        public static float Envelope(float t, int totalSamples, int sampleRate, float attack, float decay)
        {
            float duration = totalSamples / (float)sampleRate;
            float env = t < attack ? t / attack : (float)Math.Exp(-(t - attack) / Math.Max(0.001f, decay));
            float tail = Math.Min(0.02f, duration * 0.25f);
            if (t > duration - tail)
            {
                env *= Math.Max(0f, (duration - t) / tail);
            }

            return env;
        }

        public static float OnePoleCoefficient(float cutoffHz, int sampleRate)
        {
            return 1f - (float)Math.Exp(-2.0 * Math.PI * cutoffHz / sampleRate);
        }
    }
}
