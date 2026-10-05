using System;
using UnityEngine;
using SteepingSpirits.Brewing.Core;

namespace SteepingSpirits.Brewing.Data
{
    /// <summary>How one boil stage looks and sounds.</summary>
    [Serializable]
    public class BoilStageLook
    {
        [Tooltip("Bubbles per second")] public float bubbleRate;
        [Tooltip("Bubble diameter (world units)")] public float bubbleSize = 0.05f;
        [Tooltip("Steam puffs per second")] public float steamRate;
        [Tooltip("Placeholder noise loudness 0..1")] [Range(0f, 1f)] public float noiseVolume;
        [Tooltip("Low-pass cutoff of the placeholder noise (Hz) – higher sounds brighter")] public float noiseCutoffHz = 400f;
        [Tooltip("Short random crackles per second (shrimp/crab eyes)")] public float crackleRate;

        public BoilStageLook(float bubbleRate, float bubbleSize, float steamRate, float noiseVolume,
            float noiseCutoffHz, float crackleRate)
        {
            this.bubbleRate = bubbleRate;
            this.bubbleSize = bubbleSize;
            this.steamRate = steamRate;
            this.noiseVolume = noiseVolume;
            this.noiseCutoffHz = noiseCutoffHz;
            this.crackleRate = crackleRate;
        }
    }

    /// <summary>Feedback tuning (everything soft: no shake, no flashes).</summary>
    [Serializable]
    public class PresentationParams
    {
        [Tooltip("One entry per BoilStage: Still, Shrimp, Crab, Fish, Pearls, Raging")]
        public BoilStageLook[] stages =
        {
            new BoilStageLook(0f, 0.04f, 0.3f, 0.02f, 250f, 0f),
            new BoilStageLook(4f, 0.035f, 0.6f, 0.05f, 500f, 3f),
            new BoilStageLook(8f, 0.055f, 1.5f, 0.08f, 900f, 6f),
            new BoilStageLook(12f, 0.09f, 3f, 0.11f, 1300f, 2f),
            new BoilStageLook(20f, 0.08f, 5f, 0.14f, 1800f, 0f),
            new BoilStageLook(40f, 0.13f, 8f, 0.2f, 2600f, 0f)
        };

        [Header("Ton")]
        [Tooltip("Seconds to crossfade between stage sounds")] public float audioCrossfadeSeconds = 1.2f;
        [Range(0f, 1f)] public float masterVolume = 0.6f;
        [Tooltip("Pouring pitch at empty → full vessel")] public Vector2 pourPitchRange = new Vector2(0.8f, 1.6f);

        [Header("Aufguss")]
        [Tooltip("How much bitterness darkens the liquor (per unit B)")] public float bitterDarkening = 0.8f;
        [Tooltip("How much bitterness makes the liquor hazy (per unit B)")] public float bitterHaze = 0.6f;
        [Tooltip("Leaf scale folded → unfolded")] public Vector2 leafScaleRange = new Vector2(0.35f, 1f);
        [Tooltip("Leaf rotation while unfolding (degrees)")] public float leafUnfoldRotation = 40f;
        [Tooltip("Aroma wisps per second per unit dA/dt")] public float aromaWispsPerRate = 60f;

        [Header("Ergebnis")]
        public float resultFadeSeconds = 0.8f;
        [Tooltip("Perfect cup: slow-motion length (unscaled seconds)")] public float perfectSlowMoSeconds = 0.4f;
        [Range(0.1f, 1f)] public float perfectTimeScale = 0.6f;

        [Header("Haptik (nur Gamepad)")]
        [Range(0f, 1f)] public float hapticStrength = 0.12f;
        public float hapticSeconds = 0.12f;

        public BoilStageLook Look(BoilStage stage)
        {
            int i = Mathf.Clamp((int)stage, 0, stages.Length - 1);
            return stages[i];
        }
    }

    /// <summary>All brewing numbers except per-tea values. One asset per project.</summary>
    [CreateAssetMenu(fileName = "BrewingTuning", menuName = "SteepingSpirits/Brewing/Brewing Tuning")]
    public class BrewingTuning : ScriptableObject
    {
        [Tooltip("Simulation: water, boil stages, extraction curves, pouring, quality, timings")]
        public BrewConfig simulation = new BrewConfig();

        [Tooltip("Feedback: particles, sound, colours, result")]
        public PresentationParams presentation = new PresentationParams();
    }
}
