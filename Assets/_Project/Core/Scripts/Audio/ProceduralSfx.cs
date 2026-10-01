using UnityEngine;

namespace SteepingSpirits.Core.Audio
{
    /// <summary>
    /// Erzeugt kurze Sound-Effekte per Code (Sinus-Töne mit Hüllkurve), damit
    /// Quest-, Kampf- und UI-Sounds ohne importierte Audio-Assets funktionieren. Für
    /// „richtige" Sounds später einfach echte AudioClips einsetzen.
    /// </summary>
    public static class ProceduralSfx
    {
        private const int SampleRate = 44100;

        public static AudioClip Tone(string name, float frequency, float duration, float volume = 0.5f)
        {
            return Melody(name, new[] { frequency }, duration, volume);
        }

        /// <summary>Baut aus mehreren Frequenzen eine kleine Tonfolge (Arpeggio).</summary>
        public static AudioClip Melody(string name, float[] frequencies, float totalDuration, float volume = 0.5f)
        {
            if (frequencies == null || frequencies.Length == 0)
            {
                frequencies = new[] { 440f };
            }

            int totalSamples = Mathf.Max(1, (int)(SampleRate * totalDuration));
            var data = new float[totalSamples];
            int noteSamples = Mathf.Max(1, totalSamples / frequencies.Length);

            for (int n = 0; n < frequencies.Length; n++)
            {
                int start = n * noteSamples;
                int end = (n == frequencies.Length - 1) ? totalSamples : Mathf.Min(totalSamples, start + noteSamples);
                float noteDuration = (end - start) / (float)SampleRate;
                float freq = frequencies[n];

                for (int i = start; i < end; i++)
                {
                    float t = (i - start) / (float)SampleRate;
                    float wave = Mathf.Sin(2f * Mathf.PI * freq * t);
                    data[i] = wave * Envelope(t, noteDuration) * volume;
                }
            }

            var clip = AudioClip.Create(name, totalSamples, 1, SampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }

        // Kurzer Anschlag + Ausklang, damit es nicht klickt.
        private static float Envelope(float t, float duration)
        {
            const float attack = 0.005f;
            float release = Mathf.Min(0.08f, duration * 0.5f);

            if (t < attack)
            {
                return t / attack;
            }
            if (t > duration - release)
            {
                return Mathf.Max(0f, (duration - t) / release);
            }
            return 1f;
        }
    }
}
