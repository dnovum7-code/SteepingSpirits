using System.Collections.Generic;
using UnityEngine;

namespace SteepingSpirits.Plantation
{
    /// <summary>
    /// Quiet procedural placeholder sounds for the garden (made once, cached). Everything is soft:
    /// low-passed noise and gentle sine plucks with short fades, no clicks, nothing shrill.
    /// </summary>
    public static class GardenSynth
    {
        private const int Rate = 22050;
        private static readonly Dictionary<GardenSound, AudioClip> cache = new Dictionary<GardenSound, AudioClip>();

        public static AudioClip Clip(GardenSound sound)
        {
            if (cache.TryGetValue(sound, out AudioClip clip) && clip != null) return clip;
            clip = Make(sound);
            cache[sound] = clip;
            return clip;
        }

        private static AudioClip Make(GardenSound sound)
        {
            switch (sound)
            {
                case GardenSound.Hoe: return Noise("garden_hoe", 0.16f, 0.08f, 0.55f, 3, 11);
                case GardenSound.Sow: return Pluck("garden_sow", 0.14f, new[] { 880f }, 0.25f);
                case GardenSound.Water: return Noise("garden_water", 0.45f, 0.25f, 0.35f, 9, 23);
                case GardenSound.Refill: return Noise("garden_refill", 0.6f, 0.3f, 0.3f, 14, 31);
                case GardenSound.Harvest: return Pluck("garden_harvest", 0.32f, new[] { 523.25f, 783.99f }, 0.3f);
                case GardenSound.Dig: return Pluck("garden_dig", 0.2f, new[] { 130.81f }, 0.45f);
                case GardenSound.Tool: return Pluck("garden_tool", 0.06f, new[] { 1318.5f }, 0.12f);
                case GardenSound.Chest: return Pluck("garden_chest", 0.3f, new[] { 392f, 493.88f, 587.33f }, 0.22f);
                case GardenSound.EndDay: return Pluck("garden_endday", 1.2f, new[] { 523.25f, 659.25f, 783.99f }, 0.2f);
                default: return Pluck("garden_grow", 0.8f, new[] { 1046.5f, 1318.5f }, 0.1f);
            }
        }

        /// <summary>Low-passed noise; "drops" adds soft little blips (crumbs or water drops).</summary>
        private static AudioClip Noise(string name, float seconds, float smooth, float gain, int drops, int seed)
        {
            int n = Mathf.Max(1, (int)(seconds * Rate));
            var data = new float[n];
            var rng = new System.Random(seed);
            float lp = 0f, a = Mathf.Clamp01(1f - smooth);
            for (int i = 0; i < n; i++)
            {
                float t = i / (float)n;
                lp += ((float)rng.NextDouble() * 2f - 1f - lp) * a * 0.25f;
                data[i] = lp * gain * Envelope(t, 0.08f);
            }

            for (int d = 0; d < drops; d++)
            {
                int start = rng.Next(0, Mathf.Max(1, n - Rate / 30));
                float f = 600f + (float)rng.NextDouble() * 900f;
                int len = Rate / 30;
                for (int i = 0; i < len && start + i < n; i++)
                {
                    float t = i / (float)len;
                    data[start + i] += Mathf.Sin(2f * Mathf.PI * f * i / Rate) * 0.12f * (1f - t) * (1f - t);
                }
            }

            return Create(name, data);
        }

        /// <summary>Soft sine plucks, one after the other (chords become little arpeggios).</summary>
        private static AudioClip Pluck(string name, float seconds, float[] notes, float gain)
        {
            int n = Mathf.Max(1, (int)(seconds * Rate));
            var data = new float[n];
            float step = notes.Length > 1 ? seconds * 0.18f : 0f;
            for (int k = 0; k < notes.Length; k++)
            {
                int offset = (int)(k * step * Rate);
                for (int i = offset; i < n; i++)
                {
                    float t = (i - offset) / (float)Rate;
                    float env = Mathf.Exp(-t * 7f / Mathf.Max(0.1f, seconds)) * Mathf.Clamp01(t * 200f);
                    data[i] += (Mathf.Sin(2f * Mathf.PI * notes[k] * t) + 0.25f * Mathf.Sin(4f * Mathf.PI * notes[k] * t)) * env * gain / notes.Length;
                }
            }

            for (int i = 0; i < n; i++) data[i] *= Envelope(i / (float)n, 0.02f);
            return Create(name, data);
        }

        private static float Envelope(float t, float attack) => Mathf.Clamp01(t / Mathf.Max(0.001f, attack)) * Mathf.Clamp01((1f - t) / 0.2f);

        private static AudioClip Create(string name, float[] data)
        {
            AudioClip clip = AudioClip.Create(name, data.Length, 1, Rate, false);
            clip.SetData(data, 0);
            return clip;
        }
    }
}
