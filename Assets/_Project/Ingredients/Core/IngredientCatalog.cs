using System;
using System.Collections.Generic;

namespace SteepingSpirits.Ingredients
{
    public enum Rarity
    {
        Common,
        Rare
    }

    /// <summary>What an ingredient is, for brewing and sorting.</summary>
    public enum IngredientFamily
    {
        Leaf,
        Herb,
        Blossom,
        Dew,
        Water,

        /// <summary>Processed tea from the workshop.</summary>
        Tea
    }

    /// <summary>One entry of the shared ingredient catalogue (data only, no engine types).</summary>
    public sealed class IngredientInfo
    {
        public readonly string Id;
        public readonly Rarity Rarity;
        public readonly IngredientFamily Family;

        /// <summary>For rare ingredients: the common ingredient they are a variant of (else the id itself).</summary>
        public readonly string CommonId;

        public IngredientInfo(string id, Rarity rarity, IngredientFamily family, string commonId)
        {
            Id = id;
            Rarity = rarity;
            Family = family;
            CommonId = commonId ?? id;
        }

        public bool IsRare => Rarity == Rarity.Rare;
    }

    /// <summary>
    /// The shared ingredient catalogue of Steeping Spirits. Every module that
    /// collects, stores or brews ingredients uses these ids. Display names live
    /// in <see cref="IngredientTexts"/>.
    /// </summary>
    public static class IngredientCatalog
    {
        public const string TeaLeaf = "tea_leaf";
        public const string Herb = "herb";
        public const string Blossom = "blossom";
        public const string MorningDew = "morning_dew";
        public const string SpringWater = "spring_water";

        public const string GoldenTip = "golden_tip";
        public const string MoonHerb = "moon_herb";
        public const string SpiritBlossom = "spirit_blossom";
        public const string StarDew = "star_dew";
        public const string SpringCrystal = "spring_crystal";

        // Grown in the tea garden (Plantation module).
        public const string Mint = "mint";
        public const string Chamomile = "chamomile";
        public const string Jasmine = "jasmine";
        public const string Lavender = "lavender";
        public const string LemonBalm = "lemon_balm";

        // Made in the tea workshop (Processing module), plus the first-flush leaves it can use.
        public const string WhiteTea = "tea_white";
        public const string GreenTea = "tea_green";
        public const string OolongTea = "tea_oolong";
        public const string BlackTea = "tea_black";
        public const string FirstFlushLeaf = "tea_leaf_first";

        private static readonly IngredientInfo[] all =
        {
            new IngredientInfo(TeaLeaf, Rarity.Common, IngredientFamily.Leaf, null),
            new IngredientInfo(Herb, Rarity.Common, IngredientFamily.Herb, null),
            new IngredientInfo(Blossom, Rarity.Common, IngredientFamily.Blossom, null),
            new IngredientInfo(MorningDew, Rarity.Common, IngredientFamily.Dew, null),
            new IngredientInfo(SpringWater, Rarity.Common, IngredientFamily.Water, null),
            new IngredientInfo(GoldenTip, Rarity.Rare, IngredientFamily.Leaf, TeaLeaf),
            new IngredientInfo(MoonHerb, Rarity.Rare, IngredientFamily.Herb, Herb),
            new IngredientInfo(SpiritBlossom, Rarity.Rare, IngredientFamily.Blossom, Blossom),
            new IngredientInfo(StarDew, Rarity.Rare, IngredientFamily.Dew, MorningDew),
            new IngredientInfo(SpringCrystal, Rarity.Rare, IngredientFamily.Water, SpringWater),
            new IngredientInfo(Mint, Rarity.Common, IngredientFamily.Herb, null),
            new IngredientInfo(Chamomile, Rarity.Common, IngredientFamily.Blossom, null),
            new IngredientInfo(Jasmine, Rarity.Common, IngredientFamily.Blossom, null),
            new IngredientInfo(Lavender, Rarity.Common, IngredientFamily.Blossom, null),
            new IngredientInfo(LemonBalm, Rarity.Common, IngredientFamily.Herb, null),
            new IngredientInfo(WhiteTea, Rarity.Common, IngredientFamily.Tea, null),
            new IngredientInfo(GreenTea, Rarity.Common, IngredientFamily.Tea, null),
            new IngredientInfo(OolongTea, Rarity.Common, IngredientFamily.Tea, null),
            new IngredientInfo(BlackTea, Rarity.Common, IngredientFamily.Tea, null),
            new IngredientInfo(FirstFlushLeaf, Rarity.Common, IngredientFamily.Leaf, null)
        };

        /// <summary>
        /// Older ids used elsewhere in the project, mapped onto the catalogue.
        /// "item_teeblatt" is the meadow's tea leaf pickup (inventory/quests).
        /// </summary>
        private static readonly Dictionary<string, string> aliases = new Dictionary<string, string>
        {
            ["item_teeblatt"] = TeaLeaf
        };

        private static readonly Dictionary<string, IngredientInfo> byId = BuildIndex();

        private static Dictionary<string, IngredientInfo> BuildIndex()
        {
            var d = new Dictionary<string, IngredientInfo>();
            foreach (IngredientInfo i in all) d[i.Id] = i;
            return d;
        }

        public static IReadOnlyList<IngredientInfo> All => all;

        /// <summary>Catalogue id for an id or a known alias; null if unknown.</summary>
        public static string Normalize(string id)
        {
            if (string.IsNullOrEmpty(id)) return null;
            if (byId.ContainsKey(id)) return id;
            return aliases.TryGetValue(id, out string target) ? target : null;
        }

        public static bool IsKnown(string id) => Normalize(id) != null;

        public static IngredientInfo Get(string id)
        {
            string n = Normalize(id);
            return n != null ? byId[n] : null;
        }

        public static bool IsRare(string id)
        {
            IngredientInfo i = Get(id);
            return i != null && i.IsRare;
        }

        /// <summary>The common ingredient a rare one belongs to; unknown ids are returned unchanged.</summary>
        public static string CommonOf(string id)
        {
            IngredientInfo i = Get(id);
            return i != null ? i.CommonId : id;
        }

        public static IEnumerable<string> Ids(Rarity rarity)
        {
            foreach (IngredientInfo i in all)
            {
                if (i.Rarity == rarity) yield return i.Id;
            }
        }
    }

    /// <summary>Player-facing ingredient names and descriptions (German), in one place.</summary>
    public static class IngredientTexts
    {
        public static string Name(string id)
        {
            switch (IngredientCatalog.Normalize(id))
            {
                case IngredientCatalog.TeaLeaf: return "Teeblatt";
                case IngredientCatalog.Herb: return "Wiesenkraut";
                case IngredientCatalog.Blossom: return "Blüte";
                case IngredientCatalog.MorningDew: return "Morgentau";
                case IngredientCatalog.SpringWater: return "Quellwasser";
                case IngredientCatalog.GoldenTip: return "Goldspitze";
                case IngredientCatalog.MoonHerb: return "Mondkraut";
                case IngredientCatalog.SpiritBlossom: return "Geisterblüte";
                case IngredientCatalog.StarDew: return "Sternentau";
                case IngredientCatalog.SpringCrystal: return "Quellkristall";
                case IngredientCatalog.Mint: return "Minze";
                case IngredientCatalog.Chamomile: return "Kamille";
                case IngredientCatalog.Jasmine: return "Jasmin";
                case IngredientCatalog.Lavender: return "Lavendel";
                case IngredientCatalog.LemonBalm: return "Zitronenmelisse";
                case IngredientCatalog.WhiteTea: return "Weißtee";
                case IngredientCatalog.GreenTea: return "Grüntee";
                case IngredientCatalog.OolongTea: return "Oolong";
                case IngredientCatalog.BlackTea: return "Schwarztee";
                case IngredientCatalog.FirstFlushLeaf: return "Erster Austrieb";
                default: return id ?? "";
            }
        }

        public static string Description(string id)
        {
            switch (IngredientCatalog.Normalize(id))
            {
                case IngredientCatalog.TeaLeaf: return "Frisch gepflückt, noch feucht vom Morgen.";
                case IngredientCatalog.Herb: return "Duftet nach Wiese und Sonne.";
                case IngredientCatalog.Blossom: return "Zart und ein wenig süß.";
                case IngredientCatalog.MorningDew: return "Ein Tropfen, der das Licht einfängt.";
                case IngredientCatalog.SpringWater: return "Kühl und klar aus der Quelle.";
                case IngredientCatalog.GoldenTip: return "Eine seltene Knospe mit goldenem Flaum.";
                case IngredientCatalog.MoonHerb: return "Wächst nur, wo nachts Geister wandern.";
                case IngredientCatalog.SpiritBlossom: return "Leuchtet schwach, wenn man an jemanden denkt.";
                case IngredientCatalog.StarDew: return "Tau, der vom Nachthimmel gefallen ist.";
                case IngredientCatalog.SpringCrystal: return "Ein Dank der Laternen – klingt, wenn man es schüttelt.";
                case IngredientCatalog.Mint: return "Kühl und frisch, aus dem eigenen Garten.";
                case IngredientCatalog.Chamomile: return "Kleine Sonnen, die nach Heu und Honig duften.";
                case IngredientCatalog.Jasmine: return "Öffnet sich am Abend und duftet bis in die Nacht.";
                case IngredientCatalog.Lavender: return "Beruhigt schon, wenn man daran riecht.";
                case IngredientCatalog.LemonBalm: return "Ein Hauch Zitrone, ganz ohne Säure.";
                case IngredientCatalog.WhiteTea: return "Nur gewelkt und getrocknet – still und hell.";
                case IngredientCatalog.GreenTea: return "Im Wok fixiert, bevor es dunkel werden konnte.";
                case IngredientCatalog.OolongTea: return "Halb oxidiert, zwischen Garten und Abend.";
                case IngredientCatalog.BlackTea: return "Ganz oxidiert, warm und tief.";
                case IngredientCatalog.FirstFlushLeaf: return "Die ersten Blätter des Teestrauchs, besonders zart.";
                default: return "";
            }
        }

        public static string RarityLabel(Rarity r) => r == Rarity.Rare ? "selten" : "";
    }
}
