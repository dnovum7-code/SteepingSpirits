using System;

namespace SteepingSpirits.Brewing.Core
{
    /// <summary>
    /// Steeping simulation for one infusion. Integrates aroma and bitterness
    /// step by step so the cooling vessel temperature matters:
    ///   dA/dt = kA · fA(T) · (Amax − A)
    ///   onset += fB(T) · dt / tB
    ///   dB/dt = kB · fB(T) · (1 + accel · tSinceOnset)   once onset ≥ 1
    /// Aroma uses the exact exponential step and the bitter onset is split
    /// inside a step, so results barely depend on the frame rate.
    /// </summary>
    public sealed class ExtractionModel
    {
        private readonly TeaParams tea;
        private readonly ExtractionParams shape;
        private readonly float roomTemperature;

        public ExtractionModel(TeaParams tea, ExtractionParams shape, float startTemperature,
            float roomTemperature, float aromaMax)
        {
            this.tea = tea ?? throw new ArgumentNullException(nameof(tea));
            this.shape = shape ?? throw new ArgumentNullException(nameof(shape));
            this.roomTemperature = roomTemperature;
            Temperature = startTemperature;
            StartTemperature = startTemperature;
            AromaMax = Math.Max(0f, aromaMax);
        }

        public float StartTemperature { get; }
        public float Temperature { get; private set; }
        public float AromaMax { get; }
        public float Aroma { get; private set; }
        public float Bitterness { get; private set; }

        /// <summary>Bitter onset progress 0..1 (≥ 1 = bitterness is building).</summary>
        public float BitterOnset { get; private set; }

        public float SecondsSinceOnset { get; private set; }
        public float ElapsedSeconds { get; private set; }

        /// <summary>dA/dt of the last step (drives the aroma wisps).</summary>
        public float AromaRate { get; private set; }

        public void Tick(float deltaTime)
        {
            if (deltaTime <= 0f)
            {
                return;
            }

            float fa = AromaFactor(Temperature);
            float fb = BitterFactor(Temperature);

            float before = Aroma;
            Aroma = AromaMax - (AromaMax - Aroma) * (float)Math.Exp(-tea.aromaRate * fa * deltaTime);
            AromaRate = (Aroma - before) / deltaTime;

            float active = 0f;
            if (BitterOnset < 1f)
            {
                float needed = (1f - BitterOnset) * tea.bitterOnsetSeconds / Math.Max(fb, 1e-6f);
                if (deltaTime <= needed)
                {
                    BitterOnset += fb * deltaTime / tea.bitterOnsetSeconds;
                }
                else
                {
                    BitterOnset = 1f;
                    active = deltaTime - needed;
                }
            }
            else
            {
                active = deltaTime;
            }

            if (active > 0f)
            {
                float accel = 1f + shape.bitterAcceleration * (SecondsSinceOnset + active * 0.5f);
                Bitterness += tea.bitterRate * fb * accel * active;
                SecondsSinceOnset += active;
            }

            Temperature = roomTemperature + (Temperature - roomTemperature)
                          * (float)Math.Exp(-shape.vesselCoolingRate * deltaTime);
            ElapsedSeconds += deltaTime;
        }

        /// <summary>fA: 1 in and above the window, linear down to the floor below it.</summary>
        public float AromaFactor(float temperature)
        {
            if (temperature >= tea.idealMin)
            {
                return 1f;
            }

            float floorAt = tea.idealMin - shape.aromaFalloffRange;
            if (temperature <= floorAt)
            {
                return shape.aromaFloorFactor;
            }

            float t = (temperature - floorAt) / shape.aromaFalloffRange;
            return shape.aromaFloorFactor + (1f - shape.aromaFloorFactor) * t;
        }

        /// <summary>fB: 1 in the window, rising above it (heat sensitivity), reduced below.</summary>
        public float BitterFactor(float temperature)
        {
            if (temperature > tea.idealMax)
            {
                return 1f + tea.heatSensitivity * (temperature - tea.idealMax) / shape.heatSensitivityStep;
            }

            return temperature >= tea.idealMin ? 1f : shape.bitterBelowWindowFactor;
        }
    }
}
