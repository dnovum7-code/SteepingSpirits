using System;

namespace SteepingSpirits.Brewing.Core
{
    /// <summary>
    /// Kettle water: heats with the fire on, cools exponentially without it,
    /// goes stale after boiling too long and can be calmed by ladling back.
    /// Deterministic plain C#; advance it with <see cref="Tick"/>.
    /// Stale water is a state, not a failure – it only lowers the aroma ceiling.
    /// </summary>
    public sealed class WaterModel
    {
        // Tolerance for "has reached the boiling point" (float accumulation).
        private const float BoilEpsilon = 0.0001f;

        private readonly WaterParams parameters;
        private readonly BoilStageThresholds thresholds;

        public WaterModel(WaterParams parameters, BoilStageThresholds thresholds)
        {
            this.parameters = parameters ?? throw new ArgumentNullException(nameof(parameters));
            this.thresholds = thresholds ?? throw new ArgumentNullException(nameof(thresholds));
            Refill();
        }

        /// <summary>Fires on every boil stage change: (previous, current).</summary>
        public event Action<BoilStage, BoilStage> StageChanged;

        /// <summary>Fires once when the water turns stale.</summary>
        public event Action BecameStale;

        public float Temperature { get; private set; }
        public bool HeatOn { get; private set; }
        public BoilStage Stage { get; private set; }

        /// <summary>Seconds the water has been boiling without interruption.</summary>
        public float BoilingSeconds { get; private set; }

        public bool IsBoiling => HeatOn && Temperature >= parameters.boilingPoint - BoilEpsilon;
        public bool IsStale { get; private set; }

        /// <summary>Upper bound for aroma extraction with this water (1 = fresh).</summary>
        public float AromaCeiling => IsStale ? parameters.staleAromaFactor : 1f;

        public void SetHeat(bool on)
        {
            HeatOn = on;
        }

        public void ToggleHeat()
        {
            HeatOn = !HeatOn;
        }

        /// <summary>
        /// Ladle water back (Lu Yu): lowers the temperature and resets the boil
        /// counter. Stale water stays stale.
        /// </summary>
        public void LadleBack()
        {
            Temperature = Math.Max(parameters.roomTemperature, Temperature - parameters.ladleCooling);
            BoilingSeconds = 0f;
            UpdateStage();
        }

        /// <summary>Fresh, cold water: room temperature, not stale, fire off.</summary>
        public void Refill()
        {
            Temperature = parameters.roomTemperature;
            HeatOn = false;
            BoilingSeconds = 0f;
            IsStale = false;
            UpdateStage();
        }

        public void Tick(float deltaTime)
        {
            if (deltaTime <= 0f)
            {
                return;
            }

            if (HeatOn)
            {
                Temperature = Math.Min(parameters.boilingPoint, Temperature + parameters.heatingRate * deltaTime);
            }
            else
            {
                float room = parameters.roomTemperature;
                Temperature = room + (Temperature - room) * (float)Math.Exp(-parameters.coolingRate * deltaTime);
            }

            if (IsBoiling)
            {
                BoilingSeconds += deltaTime;
                if (!IsStale && BoilingSeconds > parameters.staleAfterSeconds)
                {
                    IsStale = true;
                    BecameStale?.Invoke();
                }
            }
            else
            {
                BoilingSeconds = 0f;
            }

            UpdateStage();
        }

        private void UpdateStage()
        {
            BoilStage next = thresholds.Classify(Temperature);
            if (next == Stage)
            {
                return;
            }

            BoilStage previous = Stage;
            Stage = next;
            StageChanged?.Invoke(previous, next);
        }
    }
}
