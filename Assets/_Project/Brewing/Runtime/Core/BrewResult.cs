namespace SteepingSpirits.Brewing.Core
{
    /// <summary>Everything about one finished infusion (also the telemetry record).</summary>
    public sealed class BrewResult
    {
        public string TeaId { get; internal set; }

        /// <summary>0 = first infusion of these leaves (Phase 2: later infusions).</summary>
        public int InfusionIndex { get; internal set; }

        /// <summary>Kettle temperature at the moment of pouring (°C).</summary>
        public float PourTemperature { get; internal set; }

        /// <summary>Vessel temperature when steeping started (°C).</summary>
        public float SteepStartTemperature { get; internal set; }

        public bool Prewarmed { get; internal set; }
        public bool StaleWater { get; internal set; }
        public float SteepSeconds { get; internal set; }
        public float Aroma { get; internal set; }
        public float Bitterness { get; internal set; }
        public float Q { get; internal set; }
        public float Harmony { get; internal set; }
        public QualityTier Tier { get; internal set; }
        public bool IsTart { get; internal set; }

        /// <summary>From choosing the tea to lifting the leaves (s).</summary>
        public float TotalSeconds { get; internal set; }

        public bool ThermometerUsed { get; internal set; }

        /// <summary>Main reason the cup was not perfect (for a gentle tip).</summary>
        public BrewHint Hint { get; internal set; }
    }
}
