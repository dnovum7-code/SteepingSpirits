namespace SteepingSpirits.Brewing.Core
{
    /// <summary>The single most useful tip after a cup. Texts live in the presentation layer.</summary>
    public enum BrewHint
    {
        None,
        TooShort,
        TooLong,
        TooCold,
        TooHot,
        StaleWater
    }

    public static class BrewAdvisor
    {
        /// <summary>
        /// Picks the dominant reason: temperature outside the window first, then
        /// steep time vs the calibrated optimum, then stale water.
        /// </summary>
        public static BrewHint Diagnose(TeaParams tea, CalibrationResult calibration, float steepStartTemperature,
            float steepSeconds, bool staleWater, QualityTier tier)
        {
            if (tier == QualityTier.Perfect)
            {
                return BrewHint.None;
            }

            if (steepStartTemperature > tea.idealMax + 3f) return BrewHint.TooHot;
            if (steepStartTemperature < tea.idealMin - 5f) return BrewHint.TooCold;

            float optimal = calibration.OptimalSeconds;
            if (steepSeconds < optimal * 0.75f) return BrewHint.TooShort;
            if (steepSeconds > optimal * 1.3f) return BrewHint.TooLong;

            return staleWater ? BrewHint.StaleWater : BrewHint.None;
        }
    }
}
