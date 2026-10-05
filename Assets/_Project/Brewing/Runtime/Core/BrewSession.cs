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
    ///   Result → HeatWater again for the next infusion of the same leaves (Phase 2).
    /// Input arrives as commands; time arrives via <see cref="Tick"/>.
    /// Commands that do not fit the current phase are ignored (return false) –
    /// there is no way to break or fail a brew.
    /// </summary>
    public sealed class BrewSession
    {
        private readonly BrewConfig config;
        private readonly List<TeaParams> teas;
        private readonly Dictionary<string, CalibrationSeries> calibrations = new Dictionary<string, CalibrationSeries>();
        private readonly List<InfusionProfile> history = new List<InfusionProfile>();

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

        /// <summary>Memory spark state changes (Visible, Caught, Missed).</summary>
        public event Action<SparkState> SparkChanged;

        public BrewConfig Config => config;
        public IReadOnlyList<TeaParams> Teas => teas;
        public BrewPhase Phase { get; private set; } = BrewPhase.SelectTea;
        public WaterModel Water { get; }

        public TeaParams Tea { get; private set; }
        public int TeaIndex { get; private set; } = -1;
        /// <summary>Calibration of the current infusion; QReference is the series reference.</summary>
        public CalibrationResult Calibration { get; private set; }

        public CalibrationSeries Series { get; private set; }
        public LeafState Leaves { get; private set; }

        /// <summary>Effective tea for the current infusion (window shifted, rates scaled).</summary>
        public TeaParams InfusionTea { get; private set; }

        /// <summary>Profiles of all finished infusions of the current leaves.</summary>
        public IReadOnlyList<InfusionProfile> History => history;

        /// <summary>The memory spark of the current steep (null outside Steep/Result).</summary>
        public MemorySpark Spark { get; private set; }

        public bool CanInfuseAgain =>
            Tea != null && Leaves != null && InfusionIndex + 1 < Math.Max(1, Tea.maxInfusions)
            && Leaves.ResidualExtract > 0.001f;

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

            return QualityEvaluator.Evaluate(Extraction.Aroma, Extraction.Bitterness, InfusionTea, config.quality,
                Calibration.QReference);
        }

        public CalibrationSeries SeriesFor(TeaParams tea)
        {
            if (!calibrations.TryGetValue(tea.id, out CalibrationSeries series))
            {
                series = BrewCalibration.CalibrateSeries(tea, config.extraction, config.quality, config.water.roomTemperature);
                calibrations[tea.id] = series;
            }

            return series;
        }

        /// <summary>Forget cached calibrations (after live tuning changes).</summary>
        public void Recalibrate()
        {
            calibrations.Clear();
            if (Tea != null)
            {
                Series = SeriesFor(Tea);
                UpdateInfusionCalibration();
            }
        }

        private void UpdateInfusionCalibration()
        {
            InfusionTea = Tea.ForInfusion(InfusionIndex);
            InfusionCalibration inf = Series.For(InfusionIndex);
            Calibration = new CalibrationResult(Series.QReference, inf.OptimalSeconds, inf.StartTemperature);
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
            Series = SeriesFor(Tea);
            Leaves = new LeafState(Tea.leafCapacity);
            InfusionIndex = 0;
            history.Clear();
            UpdateInfusionCalibration();
            VesselPrewarmed = false;
            Extraction = null;
            Spark = null;
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
            if (Phase != BrewPhase.HeatWater || !Water.TakePour())
            {
                return false; // nothing happens without enough water – refill instead
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
            float steep = Extraction.ElapsedSeconds;
            float bonus = Spark != null ? Spark.Bonus(steep) : 0f;
            float harmony = Math.Min(1f, q.Harmony + bonus);
            QualityTier tier = QualityEvaluator.Classify(harmony, config.quality);

            // The leaves lose what this infusion dissolved.
            Leaves.ResidualExtract = Math.Max(0f, Leaves.ResidualExtract - Extraction.Aroma);

            InfusionCharacter character = InfusionProfile.Classify(InfusionIndex, Extraction.Aroma,
                Extraction.Bitterness, InfusionTea.bitterTolerance, config.quality);
            history.Add(new InfusionProfile
            {
                InfusionIndex = InfusionIndex,
                Aroma = Extraction.Aroma,
                Bitterness = Extraction.Bitterness,
                Harmony = harmony,
                Tier = tier,
                MemoryCaught = Spark != null && Spark.State == SparkState.Caught,
                Character = character
            });

            LastResult = new BrewResult
            {
                TeaId = Tea.id,
                InfusionIndex = InfusionIndex,
                PourTemperature = pourKettleTemperature,
                SteepStartTemperature = Extraction.StartTemperature,
                Prewarmed = VesselPrewarmed,
                StaleWater = staleAtPour,
                SteepSeconds = steep,
                Aroma = Extraction.Aroma,
                Bitterness = Extraction.Bitterness,
                Q = q.Q,
                BaseHarmony = q.Harmony,
                Harmony = harmony,
                HarmonyBonus = harmony - q.Harmony,
                Tier = tier,
                IsTart = q.IsTart,
                TotalSeconds = ElapsedSeconds - brewStartTime,
                ThermometerUsed = thermometerUsed,
                Hint = BrewAdvisor.Diagnose(InfusionTea, Calibration, Extraction.StartTemperature,
                    steep, staleAtPour, tier),
                SparkAppeared = Spark != null && Spark.State != SparkState.None,
                MemoryCaught = Spark != null && Spark.State == SparkState.Caught,
                MemoryDepth = Spark != null ? Spark.Depth(steep) : 0f,
                Character = character,
                ResidualAfter = Leaves.ResidualExtract,
                CanInfuseAgain = CanInfuseAgain
            };

            SetPhase(BrewPhase.Result);
            Finished?.Invoke(LastResult);
            return true;
        }

        /// <summary>
        /// Infuse the same leaves again: back to heating with the next infusion
        /// index. The vessel is still warm from the last infusion.
        /// </summary>
        public bool NextInfusion()
        {
            if (Phase != BrewPhase.Result || !CanInfuseAgain)
            {
                return false;
            }

            InfusionIndex++;
            UpdateInfusionCalibration();
            VesselPrewarmed = true;
            Extraction = null;
            Spark = null;
            thermometerUsed = false;
            brewStartTime = ElapsedSeconds;
            SetPhase(BrewPhase.HeatWater);
            return true;
        }

        /// <summary>Catch the memory spark while it is visible.</summary>
        public bool CatchSpark()
        {
            return Phase == BrewPhase.Steep && Spark != null && Extraction != null
                   && Spark.TryCatch(Extraction.ElapsedSeconds);
        }

        /// <summary>Fresh cold water (full kettle) – possible whenever no tea is being poured or steeped.</summary>
        public bool RefillWater()
        {
            if (Phase == BrewPhase.Pour || Phase == BrewPhase.Steep || Phase == BrewPhase.PreWarm)
            {
                return false;
            }

            Water.Refill();
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
            InfusionTea = null;
            InfusionIndex = 0;
            history.Clear();
            Extraction = null;
            Spark = null;
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
                    Spark?.Tick(deltaTime, QualityEvaluator.RawQuality(Extraction.Aroma, Extraction.Bitterness, config.quality));
                    break;
            }
        }

        private void BeginSteep()
        {
            float start = config.pour.StartTemperature(pourKettleTemperature, VesselPrewarmed);
            float aromaMax = BrewCalibration.AromaMaxFor(Tea, InfusionIndex, Leaves.ResidualExtract, staleAtPour,
                config.water.staleAromaFactor);
            Extraction = new ExtractionModel(InfusionTea, config.extraction, start, config.water.roomTemperature, aromaMax);
            Spark = new MemorySpark(config.spark, Series.For(InfusionIndex).BestQ,
                InfusionIndex >= config.spark.firstInfusion);
            Spark.StateChanged += state => SparkChanged?.Invoke(state);
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
