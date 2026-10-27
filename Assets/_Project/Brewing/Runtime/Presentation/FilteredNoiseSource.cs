using UnityEngine;

namespace SteepingSpirits.Brewing.Presentation
{
    /// <summary>
    /// Placeholder water sound: white noise through a gentle band
    /// (one-pole high-pass against rumble + three-pole low-pass for soft highs),
    /// slow random loudness swells (bubbling movement) and optional soft crackles.
    /// Targets are set from the main thread; the audio thread glides towards
    /// them, so every change is smooth. No allocations on the audio thread
    /// (state is plain fields, RNG is xorshift).
    /// </summary>
    [RequireComponent(typeof(AudioSource))]
    public class FilteredNoiseSource : MonoBehaviour
    {
        [Tooltip("Seconds to glide to new volume/cutoff targets")]
        [SerializeField] private float glideSeconds = 1.2f;

        [Tooltip("Compensates the level loss of the filtering")]
        [SerializeField] private float makeupGain = 7f;

        // Main thread → audio thread (single floats are written atomically).
        private volatile float targetVolume;
        private volatile float targetCutoffHz = 400f;
        private volatile float targetHighpassHz = 90f;
        private volatile float targetCrackleRate;
        private volatile float targetSwellRate = 2f;
        private volatile float targetSwellDepth = 0.3f;
        private volatile float glideCoefficient;

        // Audio-thread state.
        private float volume;
        private float cutoffHz = 400f;
        private float highpassHz = 90f;
        private float lp1, lp2, lp3;
        private float hpState;
        private float swell = 1f;
        private float swellTarget = 1f;
        private float swellTimer;
        private float crackleEnvelope;
        private uint rng = 0x9E3779B9u;
        private int sampleRate = 48000;

        public void SetTarget(float volume, float cutoffHz, float crackleRate)
        {
            targetVolume = Mathf.Clamp01(volume);
            targetCutoffHz = Mathf.Clamp(cutoffHz, 40f, 12000f);
            targetCrackleRate = Mathf.Max(0f, crackleRate);
        }

        /// <summary>Character of the noise: high-pass, swell speed and depth.</summary>
        public void SetTexture(float highpassHz, float swellRate, float swellDepth)
        {
            targetHighpassHz = Mathf.Clamp(highpassHz, 20f, 2000f);
            targetSwellRate = Mathf.Max(0.05f, swellRate);
            targetSwellDepth = Mathf.Clamp01(swellDepth);
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
            int frames = data.Length / Mathf.Max(1, channels);
            float glide = glideCoefficient;
            float bufferGlide = Mathf.Min(1f, glide * frames);
            float tVolume = targetVolume;
            float crackleChance = targetCrackleRate / sampleRate;
            float crackleDecay = 1f - 60f / sampleRate; // ~17 ms crackles

            // Filter corners glide per buffer (cheap), volume per sample (no zipper noise).
            cutoffHz += (targetCutoffHz - cutoffHz) * bufferGlide;
            highpassHz += (targetHighpassHz - highpassHz) * bufferGlide;
            float lpAlpha = 1f - Mathf.Exp(-2f * Mathf.PI * cutoffHz / sampleRate);
            float hpAlpha = 1f - Mathf.Exp(-2f * Mathf.PI * highpassHz / sampleRate);

            // Swells: a new random loudness target every 1/swellRate s, approached smoothly.
            float swellInterval = 1f / targetSwellRate;
            float swellDepth = targetSwellDepth;
            float swellAlpha = 1f - Mathf.Exp(-2f * Mathf.PI * targetSwellRate / sampleRate);
            float dtSample = 1f / sampleRate;

            for (int i = 0; i < data.Length; i += channels)
            {
                volume += (tVolume - volume) * glide;

                swellTimer += dtSample;
                if (swellTimer >= swellInterval)
                {
                    swellTimer -= swellInterval;
                    swellTarget = 1f - swellDepth * (NextNoise() * 0.5f + 0.5f);
                }
                swell += (swellTarget - swell) * swellAlpha;

                float x = NextNoise();
                if (crackleChance > 0f && (NextNoise() * 0.5f + 0.5f) < crackleChance)
                {
                    crackleEnvelope = 1f;
                }
                crackleEnvelope *= crackleDecay;
                x *= 0.6f + crackleEnvelope * 2.4f;

                // High-pass (one pole): remove the slow drift that sounds like rumble.
                hpState += hpAlpha * (x - hpState);
                float band = x - hpState;

                // Three-pole low-pass: soft, round top end.
                lp1 += lpAlpha * (band - lp1);
                lp2 += lpAlpha * (lp1 - lp2);
                lp3 += lpAlpha * (lp2 - lp3);

                float sample = lp3 * volume * swell * makeupGain;
                sample = sample / (1f + Mathf.Abs(sample)); // gentle soft clip

                for (int c = 0; c < channels; c++)
                {
                    data[i + c] = sample;
                }
            }
        }
    }
}
