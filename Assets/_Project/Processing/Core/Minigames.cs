using System;

namespace SteepingSpirits.Processing
{
    /// <summary>
    /// The rolling table: rhythmic kneading (one key, or one stick circle per knead). Each knead adds
    /// to the roll; an even rhythm keeps the leaves even, mistakes only make them a bit uneven.
    /// Comfort "Gleichmäßig rollen": holding the key kneads on the beat by itself.
    /// </summary>
    public sealed class RollSession
    {
        private readonly RollParams p;
        private readonly bool comfort;
        private float lastKnead = -1f;
        private float evennessSum;
        private int samples;
        private bool stopped;

        public RollSession(RollParams p, bool comfortHold = false)
        {
            this.p = p ?? new RollParams();
            comfort = comfortHold;
        }

        public float Time { get; private set; }
        public int Kneads { get; private set; }

        /// <summary>Evenness of the last knead (for soft feedback), 1 before the second knead.</summary>
        public float LastKneadEvenness { get; private set; } = 1f;

        public float Intensity => Math.Min(1f, Kneads / (float)Math.Max(1, p.kneadsForFullRoll));

        public float Evenness => samples > 0 ? evennessSum / samples : comfort ? p.comfortEvenness : 1f;

        public bool Done => stopped || Time >= p.maxSeconds || Intensity >= 1f;

        /// <summary>0..1 position in the current beat (for the pulsing guide).</summary>
        public float BeatPhase => lastKnead < 0f ? 0f : Math.Min(1f, (Time - lastKnead) / Math.Max(0.05f, p.beatSeconds));

        public void Tick(float dt, bool holding = false)
        {
            if (Done || dt <= 0f) return;
            Time += dt;
            if (comfort && holding && (lastKnead < 0f || Time - lastKnead >= p.beatSeconds)) Knead();
        }

        public void Knead()
        {
            if (Done) return;
            if (lastKnead >= 0f)
            {
                float e = comfort ? p.comfortEvenness : KneadEvenness(Time - lastKnead);
                evennessSum += e;
                samples++;
                LastKneadEvenness = e;
            }

            lastKnead = Time;
            Kneads++;
        }

        public void Stop() => stopped = true;

        public RollOutcome Outcome => new RollOutcome { intensity = Intensity, evenness = Evenness, seconds = Time };

        private float KneadEvenness(float interval)
        {
            float err = Math.Abs(interval - p.beatSeconds);
            if (err <= p.perfectTolerance) return 1f;
            if (err >= p.sloppyTolerance) return p.evennessFloor;
            float t = (err - p.perfectTolerance) / Math.Max(0.001f, p.sloppyTolerance - p.perfectTolerance);
            return 1f + (p.evennessFloor - 1f) * t;
        }
    }

    /// <summary>
    /// The wok: hold to stoke the embers, let go to cool; keep the heat inside a slowly wandering
    /// window while the leaves are turned. Calmer than Stardew's fishing bar. Deterministic per seed.
    /// </summary>
    public sealed class WokSession
    {
        private readonly WokParams p;
        private readonly float phaseA;
        private readonly float phaseB;
        private float velocity;
        private float under;
        private float inside;
        private float over;

        public WokSession(WokParams p, int seed, float startHeat = 0.3f)
        {
            this.p = p ?? new WokParams();
            uint h = (uint)seed * 2654435761u;
            phaseA = (h % 1000u) / 1000f * 6.283f;
            phaseB = (h / 1000u % 1000u) / 1000f * 6.283f;
            Heat = startHeat;
        }

        public float Time { get; private set; }
        public float Heat { get; private set; }

        public float WindowCenter => CenterAt(Time);
        public float WindowMin => WindowCenter - p.windowWidth * 0.5f;
        public float WindowMax => WindowCenter + p.windowWidth * 0.5f;
        public bool InWindow => Heat >= WindowMin && Heat <= WindowMax;
        public bool Done => Time >= p.seconds;

        public float CenterAt(float t) =>
            0.55f + p.windowSwing * (0.6f * (float)Math.Sin(p.windowSpeed * t + phaseA) + 0.4f * (float)Math.Sin(1.7f * p.windowSpeed * t + phaseB));

        public void Tick(float dt, bool stoking)
        {
            if (Done || dt <= 0f) return;
            float target = stoking ? p.heatRise : -p.heatFall;
            velocity += (target - velocity) * Math.Min(1f, p.response * dt);
            Heat = Math.Max(0f, Math.Min(1f, Heat + velocity * dt));
            Time += dt;
            if (Heat < WindowMin) under += dt;
            else if (Heat > WindowMax) over += dt;
            else inside += dt;
        }

        public WokOutcome Outcome => new WokOutcome { under = under, inside = inside, over = over, seconds = Time };
    }
}
