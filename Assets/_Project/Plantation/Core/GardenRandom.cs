namespace SteepingSpirits.Plantation
{
    /// <summary>
    /// Stateless, deterministic randomness: the same seed and inputs always give the same
    /// number, so a garden can be replayed and saved without storing a random generator.
    /// </summary>
    public static class GardenRandom
    {
        public static uint Hash(int a, int b, int c = 0, int d = 0, int e = 0)
        {
            uint h = 2166136261u;
            h = Mix(h, (uint)a);
            h = Mix(h, (uint)b);
            h = Mix(h, (uint)c);
            h = Mix(h, (uint)d);
            h = Mix(h, (uint)e);
            h ^= h >> 16;
            h *= 0x7feb352du;
            h ^= h >> 15;
            h *= 0x846ca68bu;
            h ^= h >> 16;
            return h;
        }

        /// <summary>A number in [0, 1).</summary>
        public static float Unit(int a, int b, int c = 0, int d = 0, int e = 0) => (Hash(a, b, c, d, e) >> 8) / 16777216f;

        /// <summary>An integer in [min, max] (inclusive).</summary>
        public static int Range(int min, int max, int a, int b, int c = 0, int d = 0, int e = 0)
        {
            if (max <= min) return min;
            return min + (int)(Hash(a, b, c, d, e) % (uint)(max - min + 1));
        }

        private static uint Mix(uint h, uint v)
        {
            h ^= v;
            h *= 16777619u;
            h ^= h >> 13;
            return h;
        }
    }

    /// <summary>
    /// Until the game has real weather: a deterministic rain oracle (seed + day).
    /// Replaceable by passing a different <see cref="IWeather"/> to the garden.
    /// </summary>
    public interface IWeather
    {
        bool IsRainy(int day);
    }

    public sealed class RainOracle : IWeather
    {
        private const int Salt = 0x5241494e; // "RAIN"
        private readonly int seed;
        private readonly float chance;

        public RainOracle(int seed, float chance)
        {
            this.seed = seed;
            this.chance = chance;
        }

        public bool IsRainy(int day) => chance > 0f && GardenRandom.Unit(seed, Salt, day) < chance;
    }

    /// <summary>The day counter the garden follows (adapter to the game clock in Unity).</summary>
    public interface IDayClock
    {
        /// <summary>Absolute day number (grows by one per night, never goes back).</summary>
        int Today { get; }

        /// <summary>Go to bed: the night passes and the next morning starts.</summary>
        void EndDay();
    }
}

namespace SteepingSpirits.Plantation
{
    /// <summary>A day counter without the game (simulator, tests, scene started directly).</summary>
    public sealed class ManualDayClock : IDayClock
    {
        public ManualDayClock(int today = 1)
        {
            Today = today;
        }

        public int Today { get; private set; }

        public void EndDay() => Today++;
    }
}
