using System;
using SteepingSpirits.Ingredients;

namespace SteepingSpirits.Processing
{
    /// <summary>A dried garden herb or blossom: its own ingredient ("mint_dried"), keeping the garden quality.</summary>
    public struct DriedHerb
    {
        public string herbId;
        public int tier;
        public int amount;

        /// <summary>Herbs and blossoms the workshop can dry (garden plants).</summary>
        public static readonly string[] Herbs =
        {
            IngredientCatalog.Mint, IngredientCatalog.Chamomile, IngredientCatalog.Jasmine, IngredientCatalog.Lavender, IngredientCatalog.LemonBalm
        };

        public static string DriedId(string herbId) => IngredientQuality.Plain(herbId) + "_dried";

        public static bool IsHerb(string id) => Array.IndexOf(Herbs, IngredientQuality.Plain(id)) >= 0;

        public string Id => IngredientQuality.With(DriedId(herbId), tier);

        public static DriedHerb From(LeafBatch b) => new DriedHerb { herbId = b.sourceId, tier = b.originTier, amount = b.amount };
    }
}
