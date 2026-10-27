using System;

namespace SteepingSpirits.Brewing.Core
{
    public enum SparkState
    {
        None,      // not (yet) appeared in this infusion
        Visible,   // catchable right now
        Caught,    // being followed – the bonus grows while the tea keeps steeping
        Missed     // window passed – no penalty
    }

    /// <summary>
    /// The memory spark of one infusion. Appears once when Q rises past
    /// qShareThreshold × best Q of this infusion. Catching it starts a bonus that
    /// grows from bonusOnCatch to bonusMax while the player keeps steeping –
    /// the risk is the bitterness that keeps building meanwhile.
    /// </summary>
    public sealed class MemorySpark
    {
        private readonly SparkParams parameters;
        private readonly float triggerQ;
        private float previousQ = float.MinValue;
        private float visibleTimer;
        private float caughtAtSteepTime;

        public MemorySpark(SparkParams parameters, float bestQOfInfusion, bool allowed)
        {
            this.parameters = parameters ?? throw new ArgumentNullException(nameof(parameters));
            triggerQ = bestQOfInfusion * parameters.qShareThreshold;
            Allowed = allowed && parameters.enabled && bestQOfInfusion > 0f;
        }

        public bool Allowed { get; }
        public SparkState State { get; private set; } = SparkState.None;

        /// <summary>Seconds left to catch while visible.</summary>
        public float TimeLeft => State == SparkState.Visible ? Math.Max(0f, parameters.catchWindowSeconds - visibleTimer) : 0f;

        public event Action<SparkState> StateChanged;

        /// <summary>Advance with the current Q of the steep; returns true if the state changed.</summary>
        public bool Tick(float deltaTime, float currentQ)
        {
            SparkState before = State;

            if (State == SparkState.None && Allowed && previousQ < triggerQ && currentQ >= triggerQ
                && previousQ > float.MinValue)
            {
                State = SparkState.Visible;
                visibleTimer = 0f;
            }
            else if (State == SparkState.Visible)
            {
                visibleTimer += deltaTime;
                if (visibleTimer > parameters.catchWindowSeconds)
                {
                    State = SparkState.Missed;
                }
            }

            previousQ = currentQ;
            if (State != before)
            {
                StateChanged?.Invoke(State);
                return true;
            }

            return false;
        }

        public bool TryCatch(float steepSeconds)
        {
            if (State != SparkState.Visible)
            {
                return false;
            }

            State = SparkState.Caught;
            caughtAtSteepTime = steepSeconds;
            StateChanged?.Invoke(State);
            return true;
        }

        /// <summary>0..1 – how far the memory has been followed.</summary>
        public float Depth(float steepSeconds)
        {
            if (State != SparkState.Caught)
            {
                return 0f;
            }

            float follow = Math.Max(0.001f, parameters.followSeconds);
            return Math.Min(1f, Math.Max(0f, (steepSeconds - caughtAtSteepTime) / follow));
        }

        public float Bonus(float steepSeconds)
        {
            if (State != SparkState.Caught)
            {
                return 0f;
            }

            return parameters.bonusOnCatch + (parameters.bonusMax - parameters.bonusOnCatch) * Depth(steepSeconds);
        }
    }
}
