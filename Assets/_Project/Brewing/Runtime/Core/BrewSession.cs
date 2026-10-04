using System;
using System.Collections.Generic;

namespace SteepingSpirits.Brewing.Core
{
    public enum BrewPhase
    {
        SelectTea,
        HeatWater,
        PreWarm,
        Pour,
        Steep,
        Result
    }

    /// <summary>
    /// One brewing session as a plain state machine:
    ///   SelectTea → HeatWater → (PreWarm) → Pour → Steep → Result → SelectTea
    /// Input arrives as commands; time arrives via <see cref="Tick"/>.
    /// Commands that do not fit the current phase are ignored (return false) –
    /// there is no way to break or fail a brew.
    /// </summary>
    public sealed class BrewSession
    {
        private readonly BrewConfig config;
        private readonly List<TeaParams> teas;
        private readonly Dictionary<string, CalibrationResult> calibrations = new Dictionary<string, CalibrationResult>();

        private float phaseTimer;
        private float brewStartTime;
        private bool thermometerUsed;
        private float pourKettleTemperature;
        private bool staleAtPour;

        public BrewSession(BrewConfig config, IEnumerable<TeaParams> teas)
        {
            this.config = config ?? throw new ArgumentNullException(nameof(config));
            this.teas = new List<TeaParams>(teas ?? throw new ArgumentNullException(nameof(teas)));
            if (this.teas.Count == 0)
            {
                throw new ArgumentException("At least one tea is required.", nameof(teas));
            }

            Water = new WaterModel(config.water, config.boilStages);
            Water.StageChanged += (previous, current) => BoilStageChanged?.Invoke(previous, current);
        }

        public event Action<BrewPhase, BrewPhase> PhaseChanged;
        public event Action<BoilStage, BoilStage> BoilStageChanged;
        public event Action<BrewResult> Finished;

        public BrewConfig Config => config;
        public IReadOnlyList<TeaParams> Teas => teas;
        public BrewPhase Phase { get; private set; } = BrewPhase.SelectTea;
        public WaterModel Water { get; }

        public TeaParams Tea { get; private set; }
        public int TeaIndex { get; private set; } = -1;
        public CalibrationResult Calibration { get; private set; }
        public LeafState Leaves { get; private set; }

        /// <summary>Infusion number of the current leaves (0 = first). Phase 2 hook.</summary>
        public int InfusionIndex { get; private set; }

        public bool VesselPrewarmed { get; private set; }

        /// <summary>Only valid during Steep (and kept for Result).</summary>
        public ExtractionModel Extraction { get; private set; }

        public BrewResult LastResult { get; private set; }

        /// <summary>Total simulated seconds since the session was created.</summary>
        public float ElapsedSeconds { get; private set; }

        /// <summary>0..1 progress of the current timed phase (PreWarm, Pour).</summary>
        public float PhaseProgress
        {
            get
            {
                float duration = Phase == BrewPhase.PreWarm ? config.session.prewarmSeconds
                    : Phase == BrewPhase.Pour ? config.session.pourSeconds : 0f;
                return duration > 0f ? Math.Min(1f, phaseTimer / duration) : 0f;
            }
        }

        /// <summary>Live quality if the leaves were lifted right now (debug overlay).</summary>
        public QualityEvaluation PreviewQuality()
        {
            if (Extraction == null || Tea == null)
            {
                return new QualityEvaluation(0f, 0f, QualityTier.Flat, false);
            }

            return QualityEvaluator.Evaluate(Extraction.Aroma, Extraction.Bitterness, Tea, config.quality,
                Calibration.QReference);
        }

        public CalibrationResult CalibrationFor(TeaParams tea)
        {
            if (!calibrations.TryGetValue(tea.id, out CalibrationResult result))
            {
                result = BrewCalibration.Calibrate(tea, config.extraction, config.quality, config.water.roomTemperature);
                calibrations[tea.id] = result;
            }

            return result;
        }

        /// <summary>Forget cached calibrations (after live tuning changes).</summary>
        public void Recalibrate()
        {
            calibrations.Clear();
            if (Tea != null)
            {
                Calibration = CalibrationFor(Tea);
            }
        }

        // ---------------------------------------------------------------
        // Commands
        // ---------------------------------------------------------------

        /// <summary>Choose a tea (SelectTea, or directly from Result for the next cup).</summary>
        public bool SelectTea(int index)
        {
            if ((Phase != BrewPhase.SelectTea && Phase != BrewPhase.Result) || index < 0 || index >= teas.Count)
            {
                return false;
            }

            TeaIndex = index;
            Tea = teas[index];
            Calibration = CalibrationFor(Tea);
            Leaves = new LeafState();
            InfusionIndex = 0;
            VesselPrewarmed = false;
            Extraction = null;
            thermometerUsed = false;
            brewStartTime = ElapsedSeconds;
            SetPhase(BrewPhase.HeatWater);
            return true;
        }

        public bool ToggleHeat()
        {
            if (Phase != BrewPhase.HeatWater && Phase != BrewPhase.PreWarm)
            {
                return false;
            }

            Water.ToggleHeat();
            return true;
        }

        public bool LadleBack()
        {
            if (Phase != BrewPhase.HeatWater && Phase != BrewPhase.PreWarm)
            {
                return false;
            }

            Water.LadleBack();
            return true;
        }

        public bool StartPrewarm()
        {
            if (Phase != BrewPhase.HeatWater || VesselPrewarmed)
            {
                return false;
            }

            SetPhase(BrewPhase.PreWarm);
            return true;
        }

        public bool Pour()
        {
            if (Phase != BrewPhase.HeatWater)
            {
                return false;
            }

            pourKettleTemperature = Water.Temperature;
            staleAtPour = Water.IsStale;
            SetPhase(BrewPhase.Pour);
            return true;
        }

        public bool LiftLeaves()
        {
            if (Phase != BrewPhase.Steep || Extraction == null)
            {
                return false;
            }

            QualityEvaluation q = PreviewQuality();
            LastResult = new BrewResult
            {
                TeaId = Tea.id,
                InfusionIndex = InfusionIndex,
                PourTemperature = pourKettleTemperature,
                SteepStartTemperature = Extraction.StartTemperature,
                Prewarmed = VesselPrewarmed,
                StaleWater = staleAtPour,
                SteepSeconds = Extraction.ElapsedSeconds,
                Aroma = Extraction.Aroma,
                Bitterness = Extraction.Bitterness,
                Q = q.Q,
                Harmony = q.Harmony,
                Tier = q.Tier,
                IsTart = q.IsTart,
                TotalSeconds = ElapsedSeconds - brewStartTime,
                ThermometerUsed = thermometerUsed,
                Hint = BrewAdvisor.Diagnose(Tea, Calibration, Extraction.StartTemperature,
                    Extraction.ElapsedSeconds, staleAtPour, q.Tier)
            };

            SetPhase(BrewPhase.Result);
            Finished?.Invoke(LastResult);
            return true;
        }

        /// <summary>Back to tea selection; keeps the kettle as it is.</summary>
        public bool NextCup()
        {
            if (Phase != BrewPhase.Result)
            {
                return false;
            }

            SetPhase(BrewPhase.SelectTea);
            return true;
        }

        /// <summary>Full reset: fresh cold water, back to tea selection.</summary>
        public void Restart()
        {
            Water.Refill();
            Tea = null;
            TeaIndex = -1;
            Leaves = null;
            Extraction = null;
            VesselPrewarmed = false;
            SetPhase(BrewPhase.SelectTea);
        }

        /// <summary>The presentation reports that the player looked at the thermometer.</summary>
        public void MarkThermometerUsed()
        {
            if (Phase != BrewPhase.SelectTea && Phase != BrewPhase.Result)
            {
                thermometerUsed = true;
            }
        }

        // ---------------------------------------------------------------
        // Time
        // ---------------------------------------------------------------

        public void Tick(float deltaTime)
        {
            if (deltaTime <= 0f)
            {
                return;
            }

            ElapsedSeconds += deltaTime;
            Water.Tick(deltaTime);

            switch (Phase)
            {
                case BrewPhase.PreWarm:
                    phaseTimer += deltaTime;
                    if (phaseTimer >= config.session.prewarmSeconds)
                    {
                        VesselPrewarmed = true;
                        SetPhase(BrewPhase.HeatWater);
                    }
                    break;

                case BrewPhase.Pour:
                    phaseTimer += deltaTime;
                    if (phaseTimer >= config.session.pourSeconds)
                    {
                        BeginSteep();
                    }
                    break;

                case BrewPhase.Steep:
                    Extraction.Tick(deltaTime);
                    break;
            }
        }

        private void BeginSteep()
        {
            float start = config.pour.StartTemperature(pourKettleTemperature, VesselPrewarmed);
            float aromaMax = (staleAtPour ? config.water.staleAromaFactor : 1f) * Leaves.ResidualExtract;
            Extraction = new ExtractionModel(Tea, config.extraction, start, config.water.roomTemperature, aromaMax);
            SetPhase(BrewPhase.Steep);
        }

        private void SetPhase(BrewPhase next)
        {
            BrewPhase previous = Phase;
            Phase = next;
            phaseTimer = 0f;
            if (previous != next)
            {
                PhaseChanged?.Invoke(previous, next);
            }
        }
    }
}
