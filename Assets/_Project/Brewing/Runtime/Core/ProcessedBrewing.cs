using System;

namespace SteepingSpirits.Brewing.Core
{
    /// <summary>The four base teas a workshop tea is brewed as (TeaDefinition presets).</summary>
    public enum BrewBase
    {
        White,
        Green,
        Oolong,
        Black
    }

    /// <summary>Character notes of a workshop tea, as the brewing core sees them.</summary>
    [Flags]
    public enum BrewNotes
    {
        None = 0,
        Fresh = 1,
        Floral = 2,
        Honeyed = 4,
        Malty = 8,
        Roasty = 16,
        Delicate = 32
    }

    /// <summary>What brewing needs to know about a processed tea (filled from the workshop tea id by an adapter).</summary>
    public struct ProcessedTeaInfo
    {
        public BrewBase baseTea;

        /// <summary>Oxidation 0..1 (exact value from the workshop ledger, or the sweet spot of the variety).</summary>
        public float oxidation;

        /// <summary>Workshop quality tier 1 gut, 2 fein, 3 erlesen.</summary>
        public int tier;

        public BrewNotes notes;
    }

    /// <summary>
    /// How oxidation, quality tier and notes of a workshop tea shift the brewing parameters.
    /// Every shift is small so the four base teas keep their feel. Lives in the BrewingTuning asset.
    /// </summary>
    [Serializable]
    public class ProcessedBrewParams
    {
        // 1. Oxidation moves the oolong window: darker oolong takes hotter water.
        public float oolongWindowPerOxidation = 16f;
        public float oolongPivotOxidation = 0.35f;
        public float oolongMaxShift = 6f;
        public float oolongHeatSensitivityLight = 1.2f;
        public float oolongHeatSensitivityDark = 0.8f;
        public float oolongLightOxidation = 0.1f;
        public float oolongDarkOxidation = 0.7f;

        /// <summary>Black tea below this oxidation is a little "greener": bitterness starts earlier.</summary>
        public float blackFullOxidation = 0.85f;
        public float blackUnderOxidizedOnsetDelta = -0.5f;

        // 2. Quality tier (index 0 = gut, 1 = fein, 2 = erlesen).
        public float[] tierLeafCapacity = { 0.9f, 1.0f, 1.15f };
        public float[] tierBitterTolerance = { 0f, 0.03f, 0.06f };
        public float[] tierAromaRate = { 0.95f, 1.0f, 1.05f };

        // 3. Notes.
        public float freshWindowShift = -2f;
        public float freshHeatSensitivity = 1.15f;
        public float floralFirstAromaMax = 1.1f;
        public float honeyedBitterRate = 0.9f;
        public float maltyBitterTolerance = 0.04f;
        public float roastyWindowShift = 2f;
        public float roastyHeatSensitivity = 0.85f;
        public float roastyBitterOnset = 1f;
        public float delicateLeafSaturation = 1.15f;
        public float delicateHeatSensitivity = 1.1f;

        /// <summary>All notes together move the window by at most this (°C).</summary>
        public float noteMaxShift = 4f;

        /// <summary>Hottest water there is; the window never ends above it.</summary>
        public float boilingPoint = 100f;

        /// <summary>The window keeps at least this width (°C) when it is pushed against the boiling point.</summary>
        public float minWindowWidth = 4f;

        /// <summary>
        /// Hottest steeping start the player can reach (boiling minus the loss into a pre-warmed vessel).
        /// Window centres of all infusions are kept at or below it, so "Vollendet" stays reachable.
        /// </summary>
        public float reachableTop = 97f;
    }

    /// <summary>Turns a base tea preset into the tea that is actually brewed for a workshop tea.</summary>
    public static class ProcessedBrewing
    {
        /// <summary>
        /// Pure: returns an adjusted copy of the preset (the preset itself is never changed). Tier 2 without
        /// notes at a neutral oxidation gives the preset values back unchanged.
        /// </summary>
        public static TeaParams ForProcessedTea(TeaParams preset, ProcessedTeaInfo info, ProcessedBrewParams k)
        {
            k = k ?? new ProcessedBrewParams();
            TeaParams t = preset.Clone();
            t.infusionSteps = CloneSteps(preset.infusionSteps);
            float ox = Math.Max(0f, Math.Min(1f, info.oxidation));
            float heat = 1f;

            // 1. Oxidation.
            float oxShift = 0f;
            if (info.baseTea == BrewBase.Oolong)
            {
                oxShift = Clamp(k.oolongWindowPerOxidation * (ox - k.oolongPivotOxidation), k.oolongMaxShift);
                float span = Math.Max(0.001f, k.oolongDarkOxidation - k.oolongLightOxidation);
                float s = Math.Max(0f, Math.Min(1f, (ox - k.oolongLightOxidation) / span));
                float neutral = Math.Max(0f, Math.Min(1f, (k.oolongPivotOxidation - k.oolongLightOxidation) / span));
                heat *= Lerp(k.oolongHeatSensitivityLight, k.oolongHeatSensitivityDark, s) / Lerp(k.oolongHeatSensitivityLight, k.oolongHeatSensitivityDark, neutral);
            }
            else if (info.baseTea == BrewBase.Black && ox < k.blackFullOxidation)
            {
                t.bitterOnsetSeconds += k.blackUnderOxidizedOnsetDelta;
            }

            // 2. Quality tier.
            int i = Math.Max(1, Math.Min(3, info.tier)) - 1;
            t.leafCapacity *= At(k.tierLeafCapacity, i, 1f);
            t.bitterTolerance += At(k.tierBitterTolerance, i, 0f) - At(k.tierBitterTolerance, 1, 0f);
            t.aromaRate *= At(k.tierAromaRate, i, 1f);

            // 3. Notes.
            float noteShift = 0f;
            BrewNotes n = info.notes;
            if ((n & BrewNotes.Fresh) != 0)
            {
                noteShift += k.freshWindowShift;
                heat *= k.freshHeatSensitivity;
            }

            if ((n & BrewNotes.Floral) != 0) t.infusionSteps = WithFirstAromaMax(t.infusionSteps, k.floralFirstAromaMax);
            if ((n & BrewNotes.Honeyed) != 0) t.bitterRate *= k.honeyedBitterRate;
            if ((n & BrewNotes.Malty) != 0) t.bitterTolerance += k.maltyBitterTolerance;
            if ((n & BrewNotes.Roasty) != 0)
            {
                noteShift += k.roastyWindowShift;
                heat *= k.roastyHeatSensitivity;
                t.bitterOnsetSeconds += k.roastyBitterOnset;
            }

            if ((n & BrewNotes.Delicate) != 0)
            {
                t.leafSaturation *= k.delicateLeafSaturation;
                heat *= k.delicateHeatSensitivity;
            }

            float shift = oxShift + Clamp(noteShift, k.noteMaxShift);
            t.idealMin += shift;
            t.idealMax += shift;
            if (t.idealMax > k.boilingPoint)
            {
                t.idealMax = k.boilingPoint;
                t.idealMin = Math.Min(t.idealMin, k.boilingPoint - k.minWindowWidth);
            }

            KeepReachable(t, k.reachableTop);

            // Own calibration key: the session caches calibrations by tea id.
            t.id = preset.id + "#o" + (int)Math.Round(ox * 1000f) + "q" + (i + 1) + "n" + (int)info.notes;
            t.heatSensitivity *= heat;
            t.bitterOnsetSeconds = Math.Max(1f, t.bitterOnsetSeconds);
            return t;
        }

        /// <summary>
        /// Every infusion's window centre must stay reachable: first the base window, then the wandering
        /// of later infusions (oolong) is flattened where it would climb above the reachable top.
        /// </summary>
        private static void KeepReachable(TeaParams t, float top)
        {
            float over = t.IdealMid - top;
            if (over > 0f)
            {
                t.idealMin -= over;
                t.idealMax -= over;
            }

            for (int i = 0; i < t.infusionSteps.Length; i++)
            {
                InfusionStep step = t.infusionSteps[i];
                float room = top - t.IdealMid;
                if (step.windowShift > room) step.windowShift = Math.Max(0f, room);
            }
        }

        /// <summary>Window shift (°C) the notes alone cause, after the cap.</summary>
        public static float NoteShift(BrewNotes n, ProcessedBrewParams k)
        {
            float s = 0f;
            if ((n & BrewNotes.Fresh) != 0) s += k.freshWindowShift;
            if ((n & BrewNotes.Roasty) != 0) s += k.roastyWindowShift;
            return Clamp(s, k.noteMaxShift);
        }

        private static InfusionStep[] CloneSteps(InfusionStep[] steps)
        {
            if (steps == null) return new InfusionStep[0];
            var copy = new InfusionStep[steps.Length];
            for (int i = 0; i < steps.Length; i++)
            {
                InfusionStep s = steps[i] ?? InfusionStep.Neutral;
                copy[i] = new InfusionStep(s.windowShift, s.aromaRateFactor, s.bitterRateFactor, s.bitterOnsetFactor, s.aromaMaxFactor);
            }

            return copy;
        }

        /// <summary>Floral: the first infusion smells stronger. Later infusions stay as they were.</summary>
        private static InfusionStep[] WithFirstAromaMax(InfusionStep[] steps, float factor)
        {
            if (steps.Length == 0) return new[] { new InfusionStep(0f, 1f, 1f, 1f, factor), new InfusionStep() };
            if (steps.Length == 1)
            {
                InfusionStep only = steps[0];
                return new[] { new InfusionStep(only.windowShift, only.aromaRateFactor, only.bitterRateFactor, only.bitterOnsetFactor, only.aromaMaxFactor * factor), only };
            }

            steps[0].aromaMaxFactor *= factor;
            return steps;
        }

        private static float At(float[] a, int i, float fallback) => a != null && i >= 0 && i < a.Length ? a[i] : fallback;
        private static float Clamp(float v, float max) => Math.Max(-Math.Abs(max), Math.Min(Math.Abs(max), v));
        private static float Lerp(float a, float b, float t) => a + (b - a) * t;
    }
}
