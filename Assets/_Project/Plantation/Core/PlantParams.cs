using System;
using System.Collections.Generic;

namespace SteepingSpirits.Plantation
{
    /// <summary>How a plant reacts to water.</summary>
    public enum WaterNeed
    {
        /// <summary>"durstig": grows only on watered days.</summary>
        Thirsty,

        /// <summary>"normal": one dry day per stage is forgiven, after that it waits.</summary>
        Normal,

        /// <summary>"genügsam": grows dry too; watering every second day gives the best quality.</summary>
        Frugal
    }

    /// <summary>Tuning of one plant (the data inside a PlantDefinition asset).</summary>
    [Serializable]
    public sealed class PlantParams
    {
        public string id = "";

        /// <summary>Catalog ingredient handed out on harvest.</summary>
        public string ingredientId = "";

        /// <summary>Inventory item that is sown.</summary>
        public string seedItemId = "";

        /// <summary>Growth days to leave each stage. Stage 0 is the seed; after the last entry the plant is ripe.</summary>
        public int[] stageDays = { 1, 1 };

        public WaterNeed water = WaterNeed.Normal;

        /// <summary>Perennials stay after the harvest and ripen again after <see cref="regrowDays"/>.</summary>
        public bool perennial;

        public int regrowDays = 3;

        public int yieldMin = 1;
        public int yieldMax = 2;

        /// <summary>Quality tiers added to the very first harvest (0 for all starter plants).</summary>
        public int firstHarvestQualityBonus;

        /// <summary>
        /// Ingredient of the very first harvest, if it differs ("" = <see cref="ingredientId"/>).
        /// Tea shrub: "tea_leaf_first" – the first flush ("erster Austrieb").
        /// </summary>
        public string firstHarvestIngredientId = "";

        /// <summary>Chance per harvest to also get one seed back.</summary>
        public float seedDropChance = 0.3f;

        /// <summary>Preferred seasons ("Frühling", ...) – data only, no effect yet.</summary>
        public string[] seasons = new string[0];

        /// <summary>Stage index of the ripe plant (stages are 0..RipeStage).</summary>
        public int RipeStage => stageDays.Length;

        public int StageDays(int stage) => Math.Max(1, stage >= 0 && stage < stageDays.Length ? stageDays[stage] : 1);

        public int DaysToFirstHarvest
        {
            get
            {
                int sum = 0;
                for (int i = 0; i < stageDays.Length; i++) sum += StageDays(i);
                return sum;
            }
        }

        public PlantParams Clone()
        {
            var p = (PlantParams)MemberwiseClone();
            p.stageDays = (int[])stageDays.Clone();
            p.seasons = (string[])seasons.Clone();
            return p;
        }
    }

    /// <summary>All plants the garden knows, by plant id.</summary>
    public sealed class PlantBook
    {
        private readonly Dictionary<string, PlantParams> byId = new Dictionary<string, PlantParams>();
        private readonly List<PlantParams> ordered = new List<PlantParams>();

        public IReadOnlyList<PlantParams> All => ordered;

        public void Add(PlantParams p)
        {
            if (p == null || string.IsNullOrEmpty(p.id)) return;
            if (byId.ContainsKey(p.id)) ordered.Remove(byId[p.id]);
            byId[p.id] = p;
            ordered.Add(p);
        }

        public bool Has(string id) => id != null && byId.ContainsKey(id);

        public PlantParams Get(string id) => id != null && byId.TryGetValue(id, out PlantParams p) ? p : null;

        /// <summary>The plant sown from an inventory seed item (null if none).</summary>
        public PlantParams BySeed(string seedItemId)
        {
            foreach (PlantParams p in ordered)
            {
                if (p.seedItemId == seedItemId) return p;
            }

            return null;
        }
    }
}
