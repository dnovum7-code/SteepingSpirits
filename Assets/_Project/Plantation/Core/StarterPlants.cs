using SteepingSpirits.Ingredients;

namespace SteepingSpirits.Plantation
{
    /// <summary>
    /// Start values of the six garden plants. The editor builder copies them into the
    /// PlantDefinition assets (where they are tuned); the simulator and tests use them directly.
    /// Seasons use the GameClock names ("Fruehling", "Sommer", "Herbst", "Winter").
    /// </summary>
    public static class StarterPlants
    {
        public const string Tea = "tea";
        public const string Mint = "mint";
        public const string Chamomile = "chamomile";
        public const string Jasmine = "jasmine";
        public const string Lavender = "lavender";
        public const string LemonBalm = "lemon_balm";

        public static readonly string[] Ids = { Tea, Mint, Chamomile, Jasmine, Lavender, LemonBalm };

        public static string SeedItemId(string plantId) => "seed_" + plantId;

        public static PlantParams Create(string plantId)
        {
            switch (plantId)
            {
                case Tea:
                    // Slow to the first harvest ("erster Austrieb", +1 quality), then leaves every 3 days.
                    return Make(Tea, IngredientCatalog.TeaLeaf, WaterNeed.Normal, true, new[] { 3, 3, 4, 4 }, 3, 2, 3, 1, 0.1f, "Fruehling", "Sommer");
                case Mint:
                    // Thirsty and fast: the quick reward of the first days.
                    return Make(Mint, IngredientCatalog.Mint, WaterNeed.Thirsty, false, new[] { 1, 2, 1 }, 0, 2, 3, 0, 0.6f, "Fruehling", "Sommer");
                case Chamomile:
                    return Make(Chamomile, IngredientCatalog.Chamomile, WaterNeed.Normal, false, new[] { 2, 2, 3 }, 0, 2, 4, 0, 0.6f, "Fruehling", "Sommer");
                case Jasmine:
                    // Thirsty, slow and valuable.
                    return Make(Jasmine, IngredientCatalog.Jasmine, WaterNeed.Thirsty, true, new[] { 3, 4, 4, 5 }, 5, 1, 2, 0, 0.1f, "Sommer");
                case Lavender:
                    // Forgiving: grows dry, best when watered every second day.
                    return Make(Lavender, IngredientCatalog.Lavender, WaterNeed.Frugal, true, new[] { 3, 3, 4 }, 4, 2, 3, 0, 0.15f, "Sommer");
                case LemonBalm:
                    // Reliable middle ground.
                    return Make(LemonBalm, IngredientCatalog.LemonBalm, WaterNeed.Normal, true, new[] { 2, 3, 3 }, 3, 2, 3, 0, 0.15f, "Fruehling", "Sommer", "Herbst");
                default:
                    return null;
            }
        }

        public static PlantBook Book()
        {
            var book = new PlantBook();
            foreach (string id in Ids) book.Add(Create(id));
            return book;
        }

        private static PlantParams Make(string id, string ingredient, WaterNeed water, bool perennial, int[] stages, int regrow,
            int yieldMin, int yieldMax, int firstBonus, float seedChance, params string[] seasons) => new PlantParams
        {
            id = id,
            ingredientId = ingredient,
            seedItemId = SeedItemId(id),
            stageDays = stages,
            water = water,
            perennial = perennial,
            regrowDays = perennial ? regrow : 0,
            yieldMin = yieldMin,
            yieldMax = yieldMax,
            firstHarvestQualityBonus = firstBonus,
            seedDropChance = seedChance,
            seasons = seasons
        };
    }
}
