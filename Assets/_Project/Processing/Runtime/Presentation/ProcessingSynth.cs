using System.Collections.Generic;
using UnityEngine;

namespace SteepingSpirits.Processing
{
    /// <summary>Quiet procedural placeholder sounds of the workshop (made once, cached; nothing shrill).</summary>
    public static class ProcessingSynth
    {
        private const int Rate = 22050;
        private static readonly Dictionary<WorkshopSound, AudioClip> cache = new Dictionary<WorkshopSound, AudioClip>();

        public static AudioClip Clip(WorkshopSound s)
        {
            if (cache.TryGetValue(s, out AudioClip c) && c != null) return c;
            c = Make(s);
            cache[s] = c;
            return c;
        }

        private static AudioClip Make(WorkshopSound s)
        {
            switch (s)
            {
                case WorkshopSound.Rustle: return Noise("ws_rustle", 0.5f, 0.12f, 0.22f, 0.15f, 3);
                case WorkshopSound.Knead: return Thump("ws_knead", 0.18f, 95f, 0.5f);
                case WorkshopSound.Sizzle: return Noise("ws_sizzle", 2.0f, 0.75f, 0.25f, 0.6f, 5, true);
                case WorkshopSound.Crackle: return Crackle("ws_crackle", 3.0f, 14);
                case WorkshopSound.Pack: return Pluck("ws_pack", 0.35f, new[] { 392f, 587.33f }, 0.25f);
                case WorkshopSound.Place: return Noise("ws_place", 0.22f, 0.1f, 0.25f, 0.2f, 7);
                default: return Pluck("ws_chime", 0.9f, new[] { 659.25f, 987.77f }, 0.18f);
            }
        }

        /// <summary>Filtered noise; brightness 0..1 = low-pass amount, "loop" fades nothing for seamless loops.</summary>
        private static AudioClip Noise(string name, float seconds, float brightness, float gain, float swell, int seed, bool loop = false)
        {
            int n = (int)(seconds * Rate);
            var d = new float[n];
            var rng = new System.Random(seed);
            float lp = 0f, a = Mathf.Lerp(0.03f, 0.6f, brightness);
            for (int i = 0; i < n; i++)
            {
                lp += ((float)rng.NextDouble() * 2f - 1f - lp) * a;
                float t = i / (float)n;
                float env = loop ? 1f : Mathf.Clamp01(t * 20f) * Mathf.Clamp01((1f - t) * 4f);
                float mod = 1f - swell + swell * (0.5f + 0.5f * Mathf.Sin(t * Mathf.PI * 2f * 3f));
                d[i] = lp * gain * env * mod;
            }

            return Create(name, d);
        }

        private static AudioClip Thump(string name, float seconds, float freq, float gain)
        {
            int n = (int)(seconds * Rate);
            var d = new float[n];
            for (int i = 0; i < n; i++)
            {
                float t = i / (float)Rate;
                float f = freq * (1f + 0.6f * Mathf.Exp(-t * 30f));
                d[i] = Mathf.Sin(2f * Mathf.PI * f * t) * Mathf.Exp(-t * 18f) * Mathf.Clamp01(t * 400f) * gain;
            }

            return Create(name, d);
        }

        private static AudioClip Crackle(string name, float seconds, int seed)
        {
            int n = (int)(seconds * Rate);
            var d = new float[n];
            var rng = new System.Random(seed);
            int pops = (int)(seconds * 9f);
            for (int k = 0; k < pops; k++)
            {
                int start = rng.Next(0, n - 200);
                float amp = 0.05f + (float)rng.NextDouble() * 0.12f;
                for (int i = 0; i < 160 && start + i < n; i++) d[start + i] += ((float)rng.NextDouble() * 2f - 1f) * amp * Mathf.Exp(-i / 25f);
            }

            return Create(name, d);
        }

        private static AudioClip Pluck(string name, float seconds, float[] notes, float gain)
        {
            int n = (int)(seconds * Rate);
            var d = new float[n];
            for (int k = 0; k < notes.Length; k++)
            {
                int off = (int)(k * seconds * 0.2f * Rate);
                for (int i = off; i < n; i++)
                {
                    float t = (i - off) / (float)Rate;
                    d[i] += Mathf.Sin(2f * Mathf.PI * notes[k] * t) * Mathf.Exp(-t * 6f / seconds) * Mathf.Clamp01(t * 200f) * gain / notes.Length;
                }
            }

            return Create(name, d);
        }

        private static AudioClip Create(string name, float[] data)
        {
            AudioClip clip = AudioClip.Create(name, data.Length, 1, Rate, false);
            clip.SetData(data, 0);
            return clip;
        }
    }
}
