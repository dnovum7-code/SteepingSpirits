using System;

namespace SteepingSpirits.Plantation
{
    /// <summary>Garden-wide tuning (lives in the GardenSettings asset in Unity).</summary>
    [Serializable]
    public sealed class GardenParams
    {
        /// <summary>Nights tilled, empty soil stays tilled before it turns back to untouched earth.</summary>
        public int tilledDecayDays = 3;

        /// <summary>Waterings per full can.</summary>
        public int canCapacity = 20;

        /// <summary>Chance per day that it rains (rain waters every tilled cell in the morning).</summary>
        public float rainChance = 0.15f;

        /// <summary>Seed for weather, yields and seed drops – same seed, same garden.</summary>
        public int worldSeed = 20261012;

        /// <summary>Care ratio (0..1) needed for "fein".</summary>
        public float fineCare = 0.6f;

        /// <summary>Care ratio (0..1) needed for "erlesen".</summary>
        public float excellentCare = 0.9f;

        /// <summary>Compost: quality tiers added to the first harvest of the sowing.</summary>
        public int compostQualityBonus = 1;

        /// <summary>Compost: extra pieces on the first harvest of the sowing.</summary>
        public int compostYieldBonus = 1;

        /// <summary>Every this many mornings the seed chest gets seeds of each annual plant (0 = never).</summary>
        public int chestRestockDays = 7;

        /// <summary>Seeds per annual plant in each restock.</summary>
        public int chestRestockAmount = 4;

        /// <summary>Upper bound for nights caught up at once (e.g. after a long absence).</summary>
        public int maxCatchUpDays = 56;

        public GardenParams Clone() => (GardenParams)MemberwiseClone();
    }
}
