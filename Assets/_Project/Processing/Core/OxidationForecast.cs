using System;

namespace SteepingSpirits.Processing
{
    /// <summary>
    /// "Is the colour about to turn?" – how many game minutes until an oxidizing batch would become the
    /// next (darker) tea at its current rate. Used for a calm hint, never shown as a number.
    /// </summary>
    public static class OxidationForecast
    {
        /// <summary>Game minutes until the variety changes; +infinity if it does not (not oxidizing, already black).</summary>
        public static double MinutesUntilChange(LeafBatch b, ProcessingParams p)
        {
            if (b.place != BatchPlace.OxidationBasket || b.fixedByWok || b.rollCount == 0) return double.PositiveInfinity;
            OxidationParams o = p.oxidation;
            float rate = o.ratePerMinute * (o.rollIndependentShare + (1f - o.rollIndependentShare) * b.rolled);
            rate *= Math.Max(0f, 1f - Math.Min(1f, b.dryness) / Math.Max(0.01f, o.dryStopsAt));
            if (rate <= 0f) return double.PositiveInfinity;

            TeaVariety now = TeaClassifier.Classify(b, p.variety);
            if (now == TeaVariety.Black) return double.PositiveInfinity;
            bool worked = TeaClassifier.IsWorkedGreen(b, p.variety);

            // Walk forward in small oxidation steps to find the next band (gaps included).
            const float step = 0.002f;
            for (float ox = b.oxidation + step; ox <= 1f + step; ox += step)
            {
                if (TeaClassifier.Classify(Math.Min(1f, ox), worked, p.variety) != now) return (Math.Min(1f, ox) - b.oxidation) / rate;
            }

            return double.PositiveInfinity;
        }
    }
}
