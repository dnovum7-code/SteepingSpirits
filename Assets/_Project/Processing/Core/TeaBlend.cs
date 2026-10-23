using System;
using System.Collections.Generic;
using System.Text;
using SteepingSpirits.Ingredients;

namespace SteepingSpirits.Processing
{
    /// <summary>
    /// A blend from the blending table: one workshop tea plus one dried herb or blossom
    /// (e.g. jasmine green tea). Its id: <code>tea_blend:green~jasmine/floral.fresh+q2</code>
    /// = catalogue id, base tea, herb, notes (sorted), quality (the lower of tea and herb).
    /// The herb adds its own note (blossoms floral, mint and lemon balm fresh).
    /// </summary>
    public sealed class TeaBlend
    {
        public const string HerbSeparator = "~";

        /// <summary>Portions made from one tea portion and one herb portion.</summary>
        public const int PortionsPerBlend = 2;

        public TeaVariety tea;
        public string herbId = "";
        public int tier = 1;
        public List<TeaNote> notes = new List<TeaNote>();

        public string Id
        {
            get
            {
                var sb = new StringBuilder(IngredientCatalog.TeaBlend).Append(ProcessedTea.StyleSeparator).Append(TeaKey(tea)).Append(HerbSeparator).Append(herbId);
                if (notes.Count > 0)
                {
                    sb.Append(ProcessedTea.NotesSeparator);
                    for (int i = 0; i < notes.Count; i++)
                    {
                        if (i > 0) sb.Append(ProcessedTea.NoteJoin);
                        sb.Append(TeaNotes.Key(notes[i]));
                    }
                }

                return IngredientQuality.With(sb.ToString(), tier);
            }
        }

        /// <summary>Blends a workshop tea id with a dried herb id; false if either is not usable.</summary>
        public static bool TryMake(string teaId, string driedHerbId, int maxNotes, out TeaBlend blend)
        {
            blend = null;
            if (!ProcessedTea.TryParseId(teaId, out TeaVariety v, out List<TeaNote> notes, out int teaTier)) return false;
            string plainHerb = IngredientQuality.Plain(driedHerbId) ?? "";
            if (!plainHerb.EndsWith("_dried", StringComparison.Ordinal)) return false;
            string herb = plainHerb.Substring(0, plainHerb.Length - "_dried".Length);
            if (!DriedHerb.IsHerb(herb)) return false;

            var all = new List<TeaNote> { HerbNote(herb) };
            foreach (TeaNote n in notes)
            {
                if (!all.Contains(n)) all.Add(n);
            }

            if (all.Count > Math.Max(1, maxNotes)) all.RemoveRange(Math.Max(1, maxNotes), all.Count - Math.Max(1, maxNotes));
            all.Sort();
            blend = new TeaBlend { tea = v, herbId = herb, tier = Math.Min(teaTier, IngredientQuality.Of(driedHerbId)), notes = all };
            return true;
        }

        public static TeaNote HerbNote(string herb) =>
            herb == IngredientCatalog.Mint || herb == IngredientCatalog.LemonBalm ? TeaNote.Fresh : TeaNote.Floral;

        public static bool TryParseId(string id, out TeaBlend blend)
        {
            blend = null;
            string plain = IngredientQuality.Plain(id) ?? "";
            string prefix = IngredientCatalog.TeaBlend + ProcessedTea.StyleSeparator;
            if (!plain.StartsWith(prefix, StringComparison.Ordinal)) return false;
            string rest = plain.Substring(prefix.Length);
            string notePart = "";
            int slash = rest.IndexOf(ProcessedTea.NotesSeparator);
            if (slash >= 0)
            {
                notePart = rest.Substring(slash + 1);
                rest = rest.Substring(0, slash);
            }

            int tilde = rest.IndexOf(HerbSeparator, StringComparison.Ordinal);
            if (tilde < 0) return false;
            if (!TryTea(rest.Substring(0, tilde), out TeaVariety v)) return false;
            blend = new TeaBlend { tea = v, herbId = rest.Substring(tilde + 1), tier = IngredientQuality.Of(id) };
            foreach (string key in notePart.Split(new[] { ProcessedTea.NoteJoin }, StringSplitOptions.RemoveEmptyEntries))
            {
                if (TeaNotes.TryParse(key, out TeaNote n) && !blend.notes.Contains(n)) blend.notes.Add(n);
            }

            blend.notes.Sort();
            return true;
        }

        private static string TeaKey(TeaVariety v)
        {
            switch (v)
            {
                case TeaVariety.White: return "white";
                case TeaVariety.Green: return "green";
                case TeaVariety.LightOolong: return "oolong_light";
                case TeaVariety.DarkOolong: return "oolong_dark";
                default: return "black";
            }
        }

        private static bool TryTea(string key, out TeaVariety v)
        {
            foreach (TeaVariety t in (TeaVariety[])Enum.GetValues(typeof(TeaVariety)))
            {
                if (TeaKey(t) == key)
                {
                    v = t;
                    return true;
                }
            }

            v = TeaVariety.Green;
            return false;
        }
    }
}
