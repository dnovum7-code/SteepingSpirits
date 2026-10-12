namespace SteepingSpirits.Ingredients
{
    /// <summary>
    /// Quality variants of an ingredient (tea garden harvests). Tier 1 "gut" uses the plain
    /// id, so it stacks with ingredients from elsewhere; tiers 2 "fein" and 3 "erlesen" are
    /// stored as "&lt;id&gt;+q2" / "&lt;id&gt;+q3". The catalogue itself does not know these
    /// ids (Normalize returns null), so the pantry keeps them apart from the plain ones.
    /// </summary>
    public static class IngredientQuality
    {
        public const string Suffix = "+q";
        public const int Lowest = 1;
        public const int Highest = 3;

        /// <summary>Id for an ingredient in a quality tier.</summary>
        public static string With(string id, int tier)
        {
            string plain = Plain(id);
            return tier <= Lowest ? plain : plain + Suffix + (tier > Highest ? Highest : tier);
        }

        /// <summary>The quality tier stored in an id (1 when it has none).</summary>
        public static int Of(string id)
        {
            if (string.IsNullOrEmpty(id)) return Lowest;
            int i = id.LastIndexOf(Suffix, System.StringComparison.Ordinal);
            if (i < 0 || i + Suffix.Length >= id.Length) return Lowest;
            return int.TryParse(id.Substring(i + Suffix.Length), out int tier) && tier >= Lowest && tier <= Highest ? tier : Lowest;
        }

        /// <summary>The id without its quality part.</summary>
        public static string Plain(string id)
        {
            if (string.IsNullOrEmpty(id)) return id;
            int i = id.LastIndexOf(Suffix, System.StringComparison.Ordinal);
            return i < 0 ? id : id.Substring(0, i);
        }

        public static string Label(int tier)
        {
            switch (tier)
            {
                case 3: return "erlesen";
                case 2: return "fein";
                default: return "gut";
            }
        }

        /// <summary>"Minze (erlesen)" – the plain name for tier 1.</summary>
        public static string DisplayName(string id)
        {
            int tier = Of(id);
            string name = IngredientTexts.Name(Plain(id));
            return tier <= Lowest ? name : name + " (" + Label(tier) + ")";
        }
    }
}
