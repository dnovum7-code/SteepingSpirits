using System;
using System.Collections.Generic;
using SteepingSpirits.Ingredients;
using SteepingSpirits.Processing;

namespace SteepingSpirits.SaveGame
{
    /// <summary>What kind of item a recipe describes (the runtime picks colours from it).</summary>
    public enum ItemKind
    {
        Ingredient,
        Tea,
        Blend,
        DriedHerb,
        Seed,
        Other
    }

    /// <summary>Everything needed to create an inventory item again from its id alone.</summary>
    public struct ItemRecipe
    {
        public string itemId;
        public string displayName;
        public string description;
        public int goldValue;
        public int maxStack;
        public ItemKind kind;

        /// <summary>Catalogue id behind the item (tea_oolong, mint, …) or the item id itself.</summary>
        public string baseId;

        /// <summary>Quality tier 1..3 (1 for items without quality).</summary>
        public int tier;
    }

    /// <summary>
    /// Rebuilds items created at runtime from their id: ingredient items via the ingredient id grammar
    /// (catalogue + quality + workshop tea / blend / dried herb). Values match what the modules use
    /// when they create the items. Modules with their own runtime items (garden seeds) add a resolver.
    /// </summary>
    public static class ItemRecipes
    {
        public const string IngredientPrefix = "ingredient_";
        public const string MeadowTeaLeaf = "item_teeblatt";

        private static readonly List<Func<string, ItemRecipe?>> extra = new List<Func<string, ItemRecipe?>>();

        /// <summary>Adds a resolver for module items (tried after the ingredient grammar).</summary>
        public static void Register(Func<string, ItemRecipe?> resolver)
        {
            if (resolver != null && !extra.Contains(resolver)) extra.Add(resolver);
        }

        public static void ClearExtra() => extra.Clear();

        public static bool TryDescribe(string itemId, out ItemRecipe recipe)
        {
            recipe = default;
            if (string.IsNullOrEmpty(itemId)) return false;
            if (TryIngredient(itemId, out recipe)) return true;
            foreach (Func<string, ItemRecipe?> f in extra)
            {
                ItemRecipe? r = f(itemId);
                if (r.HasValue)
                {
                    recipe = r.Value;
                    return true;
                }
            }

            return false;
        }

        private static bool TryIngredient(string itemId, out ItemRecipe r)
        {
            r = default;
            string ingredient;
            if (itemId == MeadowTeaLeaf) ingredient = IngredientCatalog.TeaLeaf;
            else if (itemId.StartsWith(IngredientPrefix, StringComparison.Ordinal)) ingredient = itemId.Substring(IngredientPrefix.Length);
            else return false;

            int tier = IngredientQuality.Of(ingredient);
            if (ProcessedTea.TryParseId(ingredient, out TeaVariety v, out List<TeaNote> notes, out int teaTier))
            {
                r = Make(itemId, ProcessingTexts.TeaName(v, notes, teaTier), IngredientTexts.Description(TeaClassifier.IngredientId(v)), 4 + 3 * (teaTier - 1), ItemKind.Tea, TeaClassifier.IngredientId(v), teaTier);
                return true;
            }

            if (TeaBlend.TryParseId(ingredient, out TeaBlend blend))
            {
                r = Make(itemId, ProcessingTexts.BlendName(blend), IngredientTexts.Description(IngredientCatalog.TeaBlend), 6 + 3 * (blend.tier - 1), ItemKind.Blend, IngredientCatalog.TeaBlend, blend.tier);
                return true;
            }

            string plain = IngredientQuality.Plain(ingredient);
            IngredientInfo info = IngredientCatalog.Get(plain);
            if (info == null) return false;
            bool dried = plain.EndsWith("_dried", StringComparison.Ordinal);
            int gold = dried ? 3 + 2 * (tier - 1) : info.IsRare ? 12 : 2 + 2 * (tier - 1);
            r = Make(itemId, IngredientQuality.DisplayName(ingredient), IngredientTexts.Description(plain), gold, dried ? ItemKind.DriedHerb : ItemKind.Ingredient, plain, tier);
            return true;
        }

        private static ItemRecipe Make(string itemId, string name, string description, int gold, ItemKind kind, string baseId, int tier) => new ItemRecipe
        {
            itemId = itemId,
            displayName = name,
            description = description,
            goldValue = gold,
            maxStack = 99,
            kind = kind,
            baseId = baseId,
            tier = tier
        };
    }
}
