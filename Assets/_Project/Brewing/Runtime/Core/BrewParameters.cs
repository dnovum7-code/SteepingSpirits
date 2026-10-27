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

        /// <summary>Total extractable substance of one portion of leaves (aroma units).</summary>
        public float leafCapacity = 3f;

        /// <summary>
        /// How quickly the aroma ceiling drops as the leaves empty:
        /// Amax = (1 − e^(−residual/s)) / (1 − e^(−capacity/s)). Larger = drops sooner.
        /// </summary>
        public float leafSaturation = 1f;

        /// <summary>How often the same leaves can be infused.</summary>
        public int maxInfusions = 3;

        /// <summary>
        /// Optional per-infusion changes (index 0 = first infusion). Infusions past
        /// the end reuse the last entry; an empty list means "no change".
        /// </summary>
        public InfusionStep[] infusionSteps = new InfusionStep[0];

        public float IdealMid => (idealMin + idealMax) * 0.5f;

        public TeaParams Clone() => (TeaParams)MemberwiseClone();

        public InfusionStep StepFor(int infusionIndex)
        {
            if (infusionSteps == null || infusionSteps.Length == 0)
            {
                return InfusionStep.Neutral;
            }

            int i = Math.Max(0, Math.Min(infusionIndex, infusionSteps.Length - 1));
            return infusionSteps[i] ?? InfusionStep.Neutral;
        }

        /// <summary>The effective tea for one infusion (window shifted, rates scaled).</summary>
        public TeaParams ForInfusion(int infusionIndex)
        {
            InfusionStep step = StepFor(infusionIndex);
            TeaParams t = Clone();
            t.idealMin += step.windowShift;
            t.idealMax += step.windowShift;
            t.aromaRate *= step.aromaRateFactor;
            t.bitterRate *= step.bitterRateFactor;
            t.bitterOnsetSeconds *= step.bitterOnsetFactor;
            return t;
        }
    }

    /// <summary>How one infusion differs from the tea's base values.</summary>
    [Serializable]
    public class InfusionStep
    {
        public static readonly InfusionStep Neutral = new InfusionStep();

        /// <summary>°C the ideal window moves for this infusion.</summary>
        public float windowShift;

        public float aromaRateFactor = 1f;
        public float bitterRateFactor = 1f;
        public float bitterOnsetFactor = 1f;

        /// <summary>Scales the aroma ceiling (leaves opening up &gt; 1, tiring &lt; 1).</summary>
        public float aromaMaxFactor = 1f;

        public InfusionStep() { }

        public InfusionStep(float windowShift, float aromaRateFactor, float bitterRateFactor,
            float bitterOnsetFactor, float aromaMaxFactor)
        {
            this.windowShift = windowShift;
            this.aromaRateFactor = aromaRateFactor;
            this.bitterRateFactor = bitterRateFactor;
            this.bitterOnsetFactor = bitterOnsetFactor;
            this.aromaMaxFactor = aromaMaxFactor;
        }
    }

    /// <summary>
    /// Memory spark: appears during a steep when Q crosses a share of this
    /// infusion's best reachable Q. Catching it gives a harmony bonus that grows
    /// while the player keeps following it (= keeps steeping, risking bitterness).
    /// Missing it costs nothing.
    /// </summary>
    [Serializable]
    public class SparkParams
    {
        public bool enabled = true;

        /// <summary>Spark appears when Q ≥ threshold × best Q of this infusion (rising).</summary>
        public float qShareThreshold = 0.9f;

        /// <summary>Seconds the spark stays catchable.</summary>
        public float catchWindowSeconds = 1.5f;

        /// <summary>Seconds of following until the full bonus is reached.</summary>
        public float followSeconds = 2.5f;

        /// <summary>Harmony bonus right after catching.</summary>
        public float bonusOnCatch = 0.03f;

        /// <summary>Harmony bonus after following for followSeconds.</summary>
        public float bonusMax = 0.1f;

        /// <summary>First infusion index on which sparks can appear.</summary>
        public int firstInfusion;
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

        /// <summary>Infusion character: below this aroma a cup counts as "faint".</summary>
        public float faintAromaThreshold = 0.3f;

        /// <summary>Infusion character: bitterness above tolerance × this share counts as "robust".</summary>
        public float robustBitterShare = 0.6f;

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
        public SparkParams spark = new SparkParams();
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
            bitterOnsetSeconds = 7.5f, bitterRate = 0.04f, heatSensitivity = 0.3f, bitterTolerance = 0.25f,
            leafCapacity = 3.0f, leafSaturation = 1.0f, maxInfusions = 3
        };

        public static TeaParams White() => new TeaParams
        {
            id = "white", idealMin = 75f, idealMax = 85f, aromaRate = 0.16f,
            bitterOnsetSeconds = 12f, bitterRate = 0.035f, heatSensitivity = 0.8f, bitterTolerance = 0.20f,
            leafCapacity = 4.0f, leafSaturation = 1.2f, maxInfusions = 4
        };

        public static TeaParams Green() => new TeaParams
        {
            id = "green", idealMin = 70f, idealMax = 80f, aromaRate = 0.30f,
            bitterOnsetSeconds = 6f, bitterRate = 0.08f, heatSensitivity = 2.0f, bitterTolerance = 0.10f,
            leafCapacity = 2.8f, leafSaturation = 1.0f, maxInfusions = 3
        };

        /// <summary>
        /// "Unfolding": rolled leaves open over several infusions. The aroma
        /// ceiling and speed grow, the good window moves up a little each time.
        /// </summary>
        public static TeaParams Oolong() => new TeaParams
        {
            id = "oolong", idealMin = 85f, idealMax = 95f, aromaRate = 0.22f,
            bitterOnsetSeconds = 9f, bitterRate = 0.035f, heatSensitivity = 0.6f, bitterTolerance = 0.20f,
            leafCapacity = 6.0f, leafSaturation = 1.5f, maxInfusions = 6,
            infusionSteps = new[]
            {
                new InfusionStep(0f, 0.75f, 1f, 1.1f, 0.7f),
                new InfusionStep(2f, 1.0f, 1f, 1.0f, 0.88f),
                new InfusionStep(4f, 1.25f, 1f, 0.9f, 1.0f),
                new InfusionStep(5f, 1.35f, 1.1f, 0.85f, 1.0f),
                new InfusionStep(6f, 1.4f, 1.2f, 0.8f, 0.9f)
            }
        };
    }
}
