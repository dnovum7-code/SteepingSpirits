using System;

namespace SteepingSpirits.Brewing.Core
{
    /// <summary>The four result tiers. There is no failure tier.</summary>
    public enum QualityTier
    {
        Flat,        // "Fad"
        Decent,      // "Ordentlich"
        Harmonious,  // "Harmonisch"
        Perfect      // "Vollendet"
    }

    /// <summary>Quality of a cup at the moment the leaves are lifted.</summary>
    public readonly struct QualityEvaluation
    {
        public QualityEvaluation(float q, float harmony, QualityTier tier, bool tart)
        {
            Q = q;
            Harmony = harmony;
            Tier = tier;
            IsTart = tart;
        }

        /// <summary>Raw Q = A − w·B.</summary>
        public float Q { get; }

        /// <summary>H = Q / Qref, clamped to 0..1.</summary>
        public float Harmony { get; }

        public QualityTier Tier { get; }

        /// <summary>Bitterness above the tea's tolerance ("herb").</summary>
        public bool IsTart { get; }
    }

    public static class QualityEvaluator
    {
        public static float RawQuality(float aroma, float bitterness, QualityParams quality)
        {
            return aroma - quality.bitterWeight * bitterness;
        }

        /// <summary>
        /// Harmony is clamped to 0..1: a pour slightly better than the calibrated
        /// reference is simply "perfect", never above it.
        /// </summary>
        public static float Harmony(float q, float qReference)
        {
            if (qReference <= 0f)
            {
                return 0f;
            }

            return Math.Max(0f, Math.Min(1f, q / qReference));
        }

        public static QualityTier Classify(float harmony, QualityParams quality)
        {
            if (harmony >= quality.perfectThreshold) return QualityTier.Perfect;
            if (harmony >= quality.harmoniousThreshold) return QualityTier.Harmonious;
            if (harmony >= quality.decentThreshold) return QualityTier.Decent;
            return QualityTier.Flat;
        }

        public static QualityEvaluation Evaluate(float aroma, float bitterness, TeaParams tea,
            QualityParams quality, float qReference)
        {
            float q = RawQuality(aroma, bitterness, quality);
            float h = Harmony(q, qReference);
            return new QualityEvaluation(q, h, Classify(h, quality), bitterness > tea.bitterTolerance);
        }
    }
}
