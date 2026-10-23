using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using SteepingSpirits.Ingredients;

namespace SteepingSpirits.Processing
{
    /// <summary>
    /// The data contract of a finished tea: base tea, oxidation, quality, character notes and origin.
    /// As an ingredient it has an id built from the catalogue id plus its properties:
    /// <code>tea_oolong:dark/honeyed.roasty+q3</code>
    /// = catalogue id, oolong style (light/dark), notes (sorted), quality (IngredientQuality, tier 1 plain).
    /// Same tea, same notes, same quality → same id → it stacks; different notes stay apart.
    /// The exact oxidation and the origin are extra data (not part of the id).
    /// </summary>
    [Serializable]
    public sealed class ProcessedTea
    {
        public const char StyleSeparator = ':';
        public const char NotesSeparator = '/';
        public const char NoteJoin = '.';

        public TeaVariety variety;
        public float oxidation;
        public int tier = 1;
        public List<TeaNote> notes = new List<TeaNote>();
        public int amount = 1;

        // Origin and history (extra data).
        public string sourceId = "";
        public int originTier = 1;
        public bool firstFlush;
        public string path = "";
        public float oxidationAtStop = -1f;
        public float activeSeconds;

        public string BaseId => TeaClassifier.IngredientId(variety);

        /// <summary>The ingredient id this tea is stored under (inventory, pantry).</summary>
        public string Id => MakeId(variety, notes, tier);

        public static ProcessedTea From(LeafBatch b, ProcessingParams p)
        {
            CareReport care = TeaQuality.Evaluate(b, p);
            return new ProcessedTea
            {
                variety = TeaClassifier.Classify(b, p.variety),
                oxidation = b.oxidation,
                tier = care.tier,
                notes = TeaNotes.For(b, p),
                amount = b.amount,
                sourceId = b.sourceId,
                originTier = b.originTier,
                firstFlush = b.firstFlush,
                path = b.PathCode(),
                oxidationAtStop = b.oxidationAtStop,
                activeSeconds = b.activeSeconds
            };
        }

        public static string MakeId(TeaVariety variety, IList<TeaNote> notes, int tier)
        {
            var sb = new StringBuilder(TeaClassifier.IngredientId(variety));
            if (variety == TeaVariety.LightOolong) sb.Append(StyleSeparator).Append("light");
            if (variety == TeaVariety.DarkOolong) sb.Append(StyleSeparator).Append("dark");
            if (notes != null && notes.Count > 0)
            {
                var sorted = new List<TeaNote>(notes);
                sorted.Sort();
                sb.Append(NotesSeparator);
                for (int i = 0; i < sorted.Count; i++)
                {
                    if (i > 0 && sorted[i] == sorted[i - 1]) continue;
                    if (i > 0) sb.Append(NoteJoin);
                    sb.Append(TeaNotes.Key(sorted[i]));
                }
            }

            return IngredientQuality.With(sb.ToString(), tier);
        }

        /// <summary>Reads an id back (unknown notes are skipped). False if it is no workshop tea.</summary>
        public static bool TryParseId(string id, out TeaVariety variety, out List<TeaNote> notes, out int tier)
        {
            variety = TeaVariety.Green;
            notes = new List<TeaNote>();
            tier = IngredientQuality.Of(id);
            string plain = IngredientQuality.Plain(id);
            if (string.IsNullOrEmpty(plain)) return false;

            string notePart = "";
            int slash = plain.IndexOf(NotesSeparator);
            if (slash >= 0)
            {
                notePart = plain.Substring(slash + 1);
                plain = plain.Substring(0, slash);
            }

            string style = "";
            int colon = plain.IndexOf(StyleSeparator);
            if (colon >= 0)
            {
                style = plain.Substring(colon + 1);
                plain = plain.Substring(0, colon);
            }

            switch (plain)
            {
                case TeaIds.White: variety = TeaVariety.White; break;
                case TeaIds.Green: variety = TeaVariety.Green; break;
                case TeaIds.Black: variety = TeaVariety.Black; break;
                case TeaIds.Oolong: variety = style == "dark" ? TeaVariety.DarkOolong : TeaVariety.LightOolong; break;
                default: return false;
            }

            foreach (string key in notePart.Split(new[] { NoteJoin }, StringSplitOptions.RemoveEmptyEntries))
            {
                if (TeaNotes.TryParse(key, out TeaNote n) && !notes.Contains(n)) notes.Add(n);
            }

            notes.Sort();
            return true;
        }

        /// <summary>Catalogue id of any workshop tea id ("tea_oolong:dark/roasty+q2" → "tea_oolong").</summary>
        public static string CatalogIdOf(string id)
        {
            string plain = IngredientQuality.Plain(id) ?? "";
            int cut = plain.IndexOfAny(new[] { StyleSeparator, NotesSeparator });
            return cut >= 0 ? plain.Substring(0, cut) : plain;
        }

        /// <summary>One line of extra data for the tea ledger / logs.</summary>
        public string Describe() =>
            Id + " ox=" + oxidation.ToString("0.00", CultureInfo.InvariantCulture) + " from " + sourceId + " q" + originTier + (firstFlush ? " first flush" : "") + " path " + path;
    }
}
