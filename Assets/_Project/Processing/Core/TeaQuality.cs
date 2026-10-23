using System;

namespace SteepingSpirits.Processing
{
    /// <summary>The care that went into a batch, step by step (each 0..1, -1 = step not done).</summary>
    public struct CareReport
    {
        public float wither;
        public float roll;
        public float oxidation;
        public float wok;

        /// <summary>Average of the steps that were done.</summary>
        public float care;

        /// <summary>Origin (garden quality) as 0..1; first flush counts as 1.</summary>
        public float origin;

        public float score;
        public int ceiling;
        public int tier;
    }

    /// <summary>
    /// Quality of the finished tea: origin plus care per step gives one of three tiers
    /// (1 gut, 2 fein, 3 erlesen). Ordinary leaves stop at "fein"; leaves of garden quality
    /// "erlesen" and the first flush lift the ceiling to "erlesen".
    /// </summary>
    public static class TeaQuality
    {
        public static CareReport Evaluate(LeafBatch b, ProcessingParams p)
        {
            TeaVariety t = TeaClassifier.Classify(b, p.variety);
            var r = new CareReport { wither = WitherCare(b, p.wither), roll = -1f, oxidation = OxidationCare(t, b.oxidation, p), wok = -1f };
            if (b.rollCount > 0) r.roll = Clamp01(b.rollEvenness);
            if (b.fixedByWok) r.wok = Clamp01(b.wokCare);

            float sum = 0f;
            int n = 0;
            foreach (float c in new[] { r.wither, r.roll, r.oxidation, r.wok })
            {
                if (c < 0f) continue;
                sum += c;
                n++;
            }

            TeaQualityParams q = p.quality;
            r.care = n > 0 ? sum / n : 1f;
            r.origin = b.firstFlush ? 1f : (Math.Max(1, Math.Min(3, b.originTier)) - 1) / 2f;
            r.score = q.originWeight * r.origin + (1f - q.originWeight) * r.care;
            int tier = r.score >= q.excellentScore ? 3 : r.score >= q.fineScore ? 2 : 1;
            r.ceiling = b.firstFlush || b.originTier >= 3 ? 3 : Math.Max(1, Math.Min(3, q.ordinaryCeiling));
            r.tier = Math.Min(tier, r.ceiling);
            return r;
        }

        public static int Tier(LeafBatch b, ProcessingParams p) => Evaluate(b, p).tier;

        /// <summary>Moisture when the leaves were worked on: inside the ideal window = 1.</summary>
        public static float WitherCare(LeafBatch b, WitherParams w)
        {
            float m = b.workMoisture >= 0f ? b.workMoisture : b.moisture;
            float d = m < w.idealMin ? w.idealMin - m : m > w.idealMax ? m - w.idealMax : 0f;
            return Clamp01(1f - d / Math.Max(0.001f, w.careFalloff));
        }

        /// <summary>How close the oxidation is to the sweet spot of the tea it became.</summary>
        public static float OxidationCare(TeaVariety t, float ox, ProcessingParams p)
        {
            VarietyParams v = p.variety;
            float sweet = TeaClassifier.SweetSpot(t, v);
            float tol;
            switch (t)
            {
                case TeaVariety.LightOolong:
                    tol = (v.lightOolongMax - v.lightOolongMin) * 0.5f;
                    break;
                case TeaVariety.DarkOolong:
                    tol = (v.darkOolongMax - v.darkOolongMin) * 0.5f;
                    break;
                case TeaVariety.Black:
                    if (ox >= sweet) return 1f;
                    tol = p.quality.blackTolerance;
                    break;
                default:
                    if (ox <= sweet) return 1f;
                    tol = p.quality.paleTolerance;
                    break;
            }

            float x = Math.Abs(ox - sweet) / Math.Max(0.001f, tol);
            return Clamp01(1f - x * x);
        }

        private static float Clamp01(float v) => v < 0f ? 0f : v > 1f ? 1f : v;
    }
}
