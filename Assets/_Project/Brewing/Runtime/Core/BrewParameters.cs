using System;

namespace SteepingSpirits.Brewing.Core
{
    /// <summary>Per-tea extraction behaviour. Serialized inside a TeaDefinition asset.</summary>
    [Serializable]
    public class TeaParams
    {
        public string id = "tea";

        /// <summary>Ideal steeping window (°C).</summary>
        public float idealMin = 90f;
        public float idealMax = 100f;

        /// <summary>kA – aroma extraction rate (1/s).</summary>
        public float aromaRate = 0.35f;

        /// <summary>tB – seconds (at factor 1) before bitterness starts.</summary>
        public float bitterOnsetSeconds = 9f;

        /// <summary>kB – bitterness rate once started.</summary>
        public float bitterRate = 0.03f;

        /// <summary>How strongly heat above the window speeds up bitterness.</summary>
        public float heatSensitivity = 0.3f;

        /// <summary>Bitterness above this marks the cup as "herb" (tart). Not a failure.</summary>
        public float bitterTolerance = 0.25f;

        public float IdealMid => (idealMin + idealMax) * 0.5f;

        public TeaParams Clone() => (TeaParams)MemberwiseClone();
    }

    /// <summary>Shape of the temperature response curves, shared by all teas.</summary>
    [Serializable]
    public class ExtractionParams
    {
        /// <summary>fA at and below (idealMin − aromaFalloffRange).</summary>
        public float aromaFloorFactor = 0.3f;

        /// <summary>°C below the window over which fA falls linearly from 1 to the floor.</summary>
        public float aromaFalloffRange = 15f;

        /// <summary>fB below the ideal window.</summary>
        public float bitterBelowWindowFactor = 0.6f;

        /// <summary>°C per heatSensitivity step above the window.</summary>
        public float heatSensitivityStep = 10f;

        /// <summary>Bitterness accelerates by this factor per second after onset.</summary>
        public float bitterAcceleration = 0.1f;

        /// <summary>Exponential cooling of the steeping vessel (1/s).</summary>
        public float vesselCoolingRate = 0.01f;
    }

    /// <summary>Pouring from kettle into the vessel.</summary>
    [Serializable]
    public class PourParams
    {
        /// <summary>Temperature loss when pouring into a cold vessel (°C).</summary>
        public float lossCold = 8f;

        /// <summary>Temperature loss when the vessel was pre-warmed (°C).</summary>
        public float lossPrewarmed = 3f;

        public float StartTemperature(float kettleTemperature, bool prewarmed)
        {
            return kettleTemperature - (prewarmed ? lossPrewarmed : lossCold);
        }
    }

    /// <summary>Harmony formula, tier thresholds and calibration settings.</summary>
    [Serializable]
    public class QualityParams
    {
        /// <summary>w in Q = A − w·B.</summary>
        public float bitterWeight = 1.5f;

        public float perfectThreshold = 0.95f;
        public float harmoniousThreshold = 0.80f;
        public float decentThreshold = 0.55f;

        /// <summary>Calibration: longest steep that is simulated (s).</summary>
        public float calibrationMaxSeconds = 60f;

        /// <summary>Calibration: fixed simulation step (s).</summary>
        public float calibrationStep = 0.02f;
    }

    /// <summary>Timing of the session steps.</summary>
    [Serializable]
    public class SessionParams
    {
        /// <summary>How long pre-warming the vessel takes (s).</summary>
        public float prewarmSeconds = 2f;

        /// <summary>How long pouring takes before steeping starts (s).</summary>
        public float pourSeconds = 1.2f;
    }

    /// <summary>Everything the simulation needs besides the teas. Serialized in the BrewingTuning asset.</summary>
    [Serializable]
    public class BrewConfig
    {
        public WaterParams water = new WaterParams();
        public BoilStageThresholds boilStages = new BoilStageThresholds();
        public ExtractionParams extraction = new ExtractionParams();
        public PourParams pour = new PourParams();
        public QualityParams quality = new QualityParams();
        public SessionParams session = new SessionParams();
    }

    /// <summary>
    /// Start values for the three Phase 1 teas. Source for the default assets the
    /// sandbox builder creates and for the tests – live tuning happens in the assets.
    /// Changes to the brief's values are explained in Docs/BREWING.md.
    /// </summary>
    public static class TeaPresets
    {
        public static TeaParams Black() => new TeaParams
        {
            id = "black", idealMin = 90f, idealMax = 100f, aromaRate = 0.35f,
            bitterOnsetSeconds = 7.5f, bitterRate = 0.04f, heatSensitivity = 0.3f, bitterTolerance = 0.25f
        };

        public static TeaParams White() => new TeaParams
        {
            id = "white", idealMin = 75f, idealMax = 85f, aromaRate = 0.16f,
            bitterOnsetSeconds = 12f, bitterRate = 0.035f, heatSensitivity = 0.8f, bitterTolerance = 0.20f
        };

        public static TeaParams Green() => new TeaParams
        {
            id = "green", idealMin = 70f, idealMax = 80f, aromaRate = 0.30f,
            bitterOnsetSeconds = 6f, bitterRate = 0.08f, heatSensitivity = 2.0f, bitterTolerance = 0.10f
        };
    }
}
