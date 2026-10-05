using System;

namespace SteepingSpirits.Brewing.Core
{
    /// <summary>Outcome of calibrating one tea.</summary>
    public readonly struct CalibrationResult
    {
        public CalibrationResult(float qReference, float optimalSeconds, float startTemperature)
        {
            QReference = qReference;
            OptimalSeconds = optimalSeconds;
            StartTemperature = startTemperature;
        }

        /// <summary>Best reachable Q (fresh water, steep starting at the window centre).</summary>
        public float QReference { get; }

        /// <summary>Steep time at which that best Q is reached.</summary>
        public float OptimalSeconds { get; }

        public float StartTemperature { get; }
    }

    /// <summary>
    /// Finds Qref per tea: simulate a steep that starts at the centre of the
    /// ideal window with fresh water and fresh leaves, and take the best Q over
    /// time. Runs once when a tea is loaded (a few thousand cheap steps).
    /// </summary>
    public static class BrewCalibration
    {
        public static CalibrationResult Calibrate(TeaParams tea, ExtractionParams shape, QualityParams quality,
            float roomTemperature)
        {
            float start = tea.IdealMid;
            var model = new ExtractionModel(tea, shape, start, roomTemperature, 1f);
            float step = Math.Max(0.001f, quality.calibrationStep);

            float bestQ = float.MinValue;
            float bestTime = 0f;
            while (model.ElapsedSeconds < quality.calibrationMaxSeconds)
            {
                model.Tick(step);
                float q = QualityEvaluator.RawQuality(model.Aroma, model.Bitterness, quality);
                if (q > bestQ)
                {
                    bestQ = q;
                    bestTime = model.ElapsedSeconds;
                }
            }

            return new CalibrationResult(bestQ, bestTime, start);
        }

        /// <summary>
        /// Calibrates all infusions of one portion of leaves: each infusion is
        /// steeped at its own window centre and lifted at its best moment; the
        /// leaves lose the dissolved aroma in between. Qref is the best Q of the
        /// whole series, so teas that open up (oolong) peak in a later infusion.
        /// </summary>
        public static CalibrationSeries CalibrateSeries(TeaParams tea, ExtractionParams shape, QualityParams quality,
            float roomTemperature)
        {
            var infusions = new System.Collections.Generic.List<InfusionCalibration>();
            float residual = tea.leafCapacity;
            float step = Math.Max(0.001f, quality.calibrationStep);
            float qReference = float.MinValue;

            for (int i = 0; i < Math.Max(1, tea.maxInfusions); i++)
            {
                TeaParams effective = tea.ForInfusion(i);
                float aromaMax = AromaMaxFor(tea, i, residual, false, 0f);
                var model = new ExtractionModel(effective, shape, effective.IdealMid, roomTemperature, aromaMax);

                float bestQ = float.MinValue, bestTime = 0f, aromaAtBest = 0f;
                while (model.ElapsedSeconds < quality.calibrationMaxSeconds)
                {
                    model.Tick(step);
                    float q = QualityEvaluator.RawQuality(model.Aroma, model.Bitterness, quality);
                    if (q > bestQ)
                    {
                        bestQ = q;
                        bestTime = model.ElapsedSeconds;
                        aromaAtBest = model.Aroma;
                    }
                }

                infusions.Add(new InfusionCalibration(bestQ, bestTime, effective.IdealMid, aromaMax));
                qReference = Math.Max(qReference, bestQ);
                residual = Math.Max(0f, residual - aromaAtBest);
            }

            return new CalibrationSeries(qReference, infusions);
        }

        /// <summary>Aroma ceiling of an infusion: what the leaves still hold, shaped by the infusion step and stale water.</summary>
        public static float AromaMaxFor(TeaParams tea, int infusionIndex, float residualExtract, bool staleWater,
            float staleFactor)
        {
            float s = Math.Max(0.01f, tea.leafSaturation);
            float full = 1f - (float)Math.Exp(-Math.Max(0.01f, tea.leafCapacity) / s);
            float now = 1f - (float)Math.Exp(-Math.Max(0f, residualExtract) / s);
            float ceiling = Math.Min(1f, now / full) * tea.StepFor(infusionIndex).aromaMaxFactor;
            return staleWater ? ceiling * staleFactor : ceiling;
        }

        /// <summary>
        /// Simulates a whole steep with a fixed lift time – handy for tests,
        /// tuning tools and the debug overlay preview.
        /// </summary>
        public static QualityEvaluation Simulate(TeaParams tea, ExtractionParams shape, QualityParams quality,
            float roomTemperature, float startTemperature, float liftSeconds, float aromaMax, float qReference)
        {
            var model = new ExtractionModel(tea, shape, startTemperature, roomTemperature, aromaMax);
            float step = Math.Max(0.001f, quality.calibrationStep);
            while (model.ElapsedSeconds + step * 0.5f < liftSeconds)
            {
                model.Tick(Math.Min(step, liftSeconds - model.ElapsedSeconds));
            }

            return QualityEvaluator.Evaluate(model.Aroma, model.Bitterness, tea, quality, qReference);
        }
    }
}
