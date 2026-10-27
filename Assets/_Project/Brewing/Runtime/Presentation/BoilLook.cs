using UnityEngine;
using SteepingSpirits.Brewing.Core;
using SteepingSpirits.Brewing.Data;

namespace SteepingSpirits.Brewing.Presentation
{
    /// <summary>Blended look values for the current water temperature.</summary>
    public struct BoilLookSample
    {
        public float bubbleRate;
        public float bubbleSize;
        public float steamRate;
        public float noiseVolume;
        public float noiseCutoffHz;
        public float crackleRate;
        public float highpassHz;
        public float swellRate;
        public float swellDepth;
    }

    /// <summary>
    /// Interpolates the per-stage looks by temperature, so bubbles, steam and
    /// sound change gradually inside a stage instead of jumping at thresholds –
    /// the player reads the water, not a number.
    /// </summary>
    public static class BoilLook
    {
        private static float Bound(int index, BoilStageThresholds th, float room, float boil)
        {
            switch (index)
            {
                case 0: return room;
                case 1: return th.shrimpEyes;
                case 2: return th.crabEyes;
                case 3: return th.fishEyes;
                case 4: return th.stringOfPearls;
                case 5: return th.ragingWaves;
                default: return boil;
            }
        }

        public static BoilLookSample Evaluate(PresentationParams look, BoilStageThresholds thresholds,
            float roomTemperature, float boilingPoint, float temperature)
        {
            // Stage i spans Bound(i)..Bound(i+1); blend towards the next stage's look.
            int stage = (int)thresholds.Classify(temperature);
            float lower = Bound(stage, thresholds, roomTemperature, boilingPoint);
            float upper = Bound(stage + 1, thresholds, roomTemperature, boilingPoint);
            float t = upper > lower ? Mathf.Clamp01((temperature - lower) / (upper - lower)) : 1f;

            BoilStageLook a = look.Look((BoilStage)stage);
            BoilStageLook b = look.Look((BoilStage)Mathf.Min(stage + 1, (int)BoilStage.RagingWaves));
            // Within a stage only blend the second half, so each stage keeps its own character.
            float w = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.5f, 1f, t));

            return new BoilLookSample
            {
                bubbleRate = Mathf.Lerp(a.bubbleRate, b.bubbleRate, w),
                bubbleSize = Mathf.Lerp(a.bubbleSize, b.bubbleSize, w),
                steamRate = Mathf.Lerp(a.steamRate, b.steamRate, w),
                noiseVolume = Mathf.Lerp(a.noiseVolume, b.noiseVolume, w),
                noiseCutoffHz = Mathf.Lerp(a.noiseCutoffHz, b.noiseCutoffHz, w),
                crackleRate = Mathf.Lerp(a.crackleRate, b.crackleRate, w),
                highpassHz = Mathf.Lerp(a.noiseHighpassHz, b.noiseHighpassHz, w),
                swellRate = Mathf.Lerp(a.swellRate, b.swellRate, w),
                swellDepth = Mathf.Lerp(a.swellDepth, b.swellDepth, w)
            };
        }
    }
}
