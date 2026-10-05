using System;
using System.Collections.Generic;
using System.Text;
using SteepingSpirits.Ingredients;

namespace SteepingSpirits.Platforming.Core
{
    /// <summary>
    /// Jump'n'Run view on the shared catalogue (<see cref="IngredientCatalog"/>):
    /// the same ids plus the level symbols. Kept so level code reads naturally.
    /// </summary>
    public static class IngredientIds
    {
        public const string TeaLeaf = IngredientCatalog.TeaLeaf;
        public const string Herb = IngredientCatalog.Herb;
        public const string Blossom = IngredientCatalog.Blossom;
        public const string MorningDew = IngredientCatalog.MorningDew;
        public const string SpringWater = IngredientCatalog.SpringWater;

        public const string GoldenTip = IngredientCatalog.GoldenTip;
        public const string MoonHerb = IngredientCatalog.MoonHerb;
        public const string SpiritBlossom = IngredientCatalog.SpiritBlossom;
        public const string StarDew = IngredientCatalog.StarDew;
        public const string SpringCrystal = IngredientCatalog.SpringCrystal;

        public static readonly string[] Common = { TeaLeaf, Herb, Blossom, MorningDew, SpringWater };
        public static readonly string[] Rare = { GoldenTip, MoonHerb, SpiritBlossom, StarDew, SpringCrystal };

        /// <summary>Level symbol → id. Lower case = common, upper case = rare.</summary>
        public static string FromSymbol(char c)
        {
            int i = "tkbmq".IndexOf(c);
            if (i >= 0) return Common[i];
            i = "TKBMQ".IndexOf(c);
            return i >= 0 ? Rare[i] : null;
        }

        public static bool IsRare(string id) => IngredientCatalog.IsRare(id);

        /// <summary>The common ingredient a rare one belongs to (for sorting on the card).</summary>
        public static string BaseOf(string id) => IngredientCatalog.CommonOf(id);
    }

    /// <summary>
    /// What the player carries home: ingredient ids and amounts, nothing else.
    /// Order of first collection is kept for display.
    /// </summary>
    public sealed class IngredientBag
    {
        private readonly Dictionary<string, int> amounts = new Dictionary<string, int>();
        private readonly List<string> order = new List<string>();

        public event Action<string, int> Added;

        public int Count(string id) => id != null && amounts.TryGetValue(id, out int n) ? n : 0;

        public int Total
        {
            get
            {
                int sum = 0;
                foreach (int n in amounts.Values) sum += n;
                return sum;
            }
        }

        public IReadOnlyList<string> Ids => order;

        public void Add(string id, int amount = 1)
        {
            if (string.IsNullOrEmpty(id) || amount <= 0)
            {
                return;
            }

            if (!amounts.ContainsKey(id))
            {
                amounts[id] = 0;
                order.Add(id);
            }

            amounts[id] += amount;
            Added?.Invoke(id, amount);
        }

        public void Merge(IngredientBag other)
        {
            foreach (string id in other.order)
            {
                Add(id, other.Count(id));
            }
        }

        /// <summary>"tea_leaf:3,herb:1" – stable order (first collected first).</summary>
        public string Serialize()
        {
            var sb = new StringBuilder();
            foreach (string id in order)
            {
                if (sb.Length > 0) sb.Append(',');
                sb.Append(id).Append(':').Append(amounts[id]);
            }

            return sb.ToString();
        }

        public static IngredientBag Parse(string text)
        {
            var bag = new IngredientBag();
            if (string.IsNullOrEmpty(text))
            {
                return bag;
            }

            foreach (string part in text.Split(','))
            {
                string[] kv = part.Split(':');
                if (kv.Length == 2 && int.TryParse(kv[1], out int n))
                {
                    bag.Add(kv[0].Trim(), n);
                }
            }

            return bag;
        }
    }

    /// <summary>One line of the end-of-level card.</summary>
    public struct IngredientTallyLine
    {
        public string id;
        public int found;
        public int available;
        public bool rare;
    }

    /// <summary>Found vs. available per ingredient for a level (end-of-level card).</summary>
    public static class IngredientTally
    {
        public static Dictionary<string, int> Available(LevelLayout layout)
        {
            var result = new Dictionary<string, int>();
            foreach (LevelMarker m in layout.All(TileKind.Ingredient))
            {
                string id = IngredientIds.FromSymbol(m.symbol);
                result.TryGetValue(id, out int n);
                result[id] = n + 1;
            }

            return result;
        }

        /// <summary>Lines sorted common → rare in catalogue order; ids with nothing available and nothing found are skipped.</summary>
        public static List<IngredientTallyLine> Lines(IngredientBag bag, IDictionary<string, int> available)
        {
            var lines = new List<IngredientTallyLine>();
            foreach (string[] group in new[] { IngredientIds.Common, IngredientIds.Rare })
            {
                foreach (string id in group)
                {
                    available.TryGetValue(id, out int avail);
                    int found = bag.Count(id);
                    if (avail == 0 && found == 0)
                    {
                        continue;
                    }

                    lines.Add(new IngredientTallyLine { id = id, found = found, available = avail, rare = IngredientIds.IsRare(id) });
                }
            }

            return lines;
        }
    }
}
