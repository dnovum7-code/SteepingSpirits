using UnityEngine;

namespace SteepingSpirits.Brewing.Presentation
{
    /// <summary>
    /// Placeholder water sound: white noise through a two-pole low-pass, plus
    /// optional soft crackles. Targets are set from the main thread; the audio
    /// thread glides towards them sample by sample, so every change is smooth.
    /// No allocations on the audio thread (state is plain fields, RNG is xorshift).
    /// </summary>
    [RequireComponent(typeof(AudioSource))]
    public class FilteredNoiseSource : MonoBehaviour
    {
        [Tooltip("Seconds to glide to new volume/cutoff targets")]
        [SerializeField] private float glideSeconds = 1.2f;

        [Tooltip("Compensates the level loss of strong low-pass filtering")]
        [SerializeField] private float makeupGain = 2.5f;

        // Main thread → audio thread (single floats are written atomically).
        private volatile float targetVolume;
        private volatile float targetCutoffHz = 400f;
        private volatile float targetCrackleRate;
        private volatile float glideCoefficient;

        // Audio-thread state.
        private float volume;
        private float cutoffHz = 400f;
        private float lp1;
        private float lp2;
        private float crackleEnvelope;
        private uint rng = 0x9E3779B9u;
        private int sampleRate = 48000;

        public void SetTarget(float volume, float cutoffHz, float crackleRate)
        {
            targetVolume = Mathf.Clamp01(volume);
            targetCutoffHz = Mathf.Clamp(cutoffHz, 40f, 12000f);
            targetCrackleRate = Mathf.Max(0f, crackleRate);
        }

        public void SetGlide(float seconds)
        {
            glideSeconds = Mathf.Max(0.01f, seconds);
            glideCoefficient = 1f - Mathf.Exp(-1f / (glideSeconds * sampleRate));
        }

        private void Awake()
        {
            sampleRate = AudioSettings.outputSampleRate > 0 ? AudioSettings.outputSampleRate : 48000;
            SetGlide(glideSeconds);

            // A silent looping clip keeps the source running; the filter writes the sound.
            AudioSource source = GetComponent<AudioSource>();
            source.clip = AudioClip.Create("silence", sampleRate, 1, sampleRate, false);
            source.loop = true;
            source.playOnAwake = false;
            source.spatialBlend = 0f;
            source.volume = 1f;
            source.Play();
        }

        private float NextNoise()
        {
            // xorshift32 → [-1, 1]
            rng ^= rng << 13;
            rng ^= rng >> 17;
            rng ^= rng << 5;
            return (rng & 0xFFFFFF) / 8388607.5f - 1f;
        }

        private void OnAudioFilterRead(float[] data, int channels)
        {
            float glide = glideCoefficient;
            float tVolume = targetVolume;
            float tCutoff = targetCutoffHz;
            float crackleChance = targetCrackleRate / sampleRate;
            float crackleDecay = 1f - 60f / sampleRate; // ~17 ms crackles

            // Cutoff glides per buffer (cheap), volume per sample (no zipper noise).
            cutoffHz += (tCutoff - cutoffHz) * Mathf.Min(1f, glide * (data.Length / Mathf.Max(1, channels)));
            float alpha = 1f - Mathf.Exp(-2f * Mathf.PI * cutoffHz / sampleRate);

            for (int i = 0; i < data.Length; i += channels)
            {
                volume += (tVolume - volume) * glide;

                float x = NextNoise();
                if (crackleChance > 0f && (NextNoise() * 0.5f + 0.5f) < crackleChance)
                {
                    crackleEnvelope = 1f;
                }
                crackleEnvelope *= crackleDecay;
                x *= 0.6f + crackleEnvelope * 2.4f;

                lp1 += alpha * (x - lp1);
                lp2 += alpha * (lp1 - lp2);
                float sample = lp2 * volume * makeupGain;

                for (int c = 0; c < channels; c++)
                {
                    data[i + c] = sample;
                }
            }
        }
    }
}
