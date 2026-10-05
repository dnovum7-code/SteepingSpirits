using System.Collections.Generic;

namespace SteepingSpirits.Brewing.Core
{
    /// <summary>Calibration of one infusion within a series.</summary>
    public readonly struct InfusionCalibration
    {
        public InfusionCalibration(float bestQ, float optimalSeconds, float startTemperature, float aromaMax)
        {
            BestQ = bestQ;
            OptimalSeconds = optimalSeconds;
            StartTemperature = startTemperature;
            AromaMax = aromaMax;
        }

        public float BestQ { get; }
        public float OptimalSeconds { get; }
        public float StartTemperature { get; }
        public float AromaMax { get; }
    }

    /// <summary>All infusions of one tea, played ideally, and the shared reference Qref.</summary>
    public sealed class CalibrationSeries
    {
        private readonly List<InfusionCalibration> infusions;

        public CalibrationSeries(float qReference, List<InfusionCalibration> infusions)
        {
            QReference = qReference;
            this.infusions = infusions;
        }

        public float QReference { get; }
        public IReadOnlyList<InfusionCalibration> Infusions => infusions;

        /// <summary>Calibration for an infusion index (past the end: the last one).</summary>
        public InfusionCalibration For(int infusionIndex)
        {
            int i = infusionIndex < 0 ? 0 : infusionIndex >= infusions.Count ? infusions.Count - 1 : infusionIndex;
            return infusions[i];
        }

        /// <summary>Index of the infusion with the best calibrated Q.</summary>
        public int BestInfusion
        {
            get
            {
                int best = 0;
                for (int i = 1; i < infusions.Count; i++)
                {
                    if (infusions[i].BestQ > infusions[best].BestQ) best = i;
                }

                return best;
            }
        }
    }
}
