using System;

namespace SteepingSpirits.Processing
{
    /// <summary>The teas the workshop makes (light and dark oolong share the catalogue id tea_oolong).</summary>
    public enum TeaVariety
    {
        White,
        Green,
        LightOolong,
        DarkOolong,
        Black
    }

    /// <summary>
    /// Which tea a batch is (or would become): from the path and the oxidation. Always a valid
    /// tea – oxidation between two bands goes to the nearer band, so too long is just darker.
    /// </summary>
    public static class TeaClassifier
    {
        public static TeaVariety Classify(LeafBatch b, VarietyParams v) => Classify(b.oxidation, IsWorkedGreen(b, v), v);

        /// <summary>Pale teas: fixed or rolled leaves are green, untouched withered leaves white.</summary>
        public static bool IsWorkedGreen(LeafBatch b, VarietyParams v) => b.fixedByWok || b.rolled >= v.rolledMin;

        public static TeaVariety Classify(float oxidation, bool workedGreen, VarietyParams v)
        {
            float ox = oxidation < 0f ? 0f : oxidation > 1f ? 1f : oxidation;
            if (ox < v.paleMax) return workedGreen ? TeaVariety.Green : TeaVariety.White;
            if (ox >= v.lightOolongMin && ox < v.lightOolongMax) return TeaVariety.LightOolong;
            if (ox >= v.darkOolongMin && ox <= v.darkOolongMax) return TeaVariety.DarkOolong;
            if (ox >= v.blackMin) return TeaVariety.Black;

            // In a gap: the nearest band wins (ties go to the darker tea – "a little further").
            TeaVariety best = TeaVariety.Black;
            float bestDistance = float.MaxValue;
            Consider(ox, 0f, v.paleMax, workedGreen ? TeaVariety.Green : TeaVariety.White, ref best, ref bestDistance);
            Consider(ox, v.lightOolongMin, v.lightOolongMax, TeaVariety.LightOolong, ref best, ref bestDistance);
            Consider(ox, v.darkOolongMin, v.darkOolongMax, TeaVariety.DarkOolong, ref best, ref bestDistance);
            Consider(ox, v.blackMin, 1f, TeaVariety.Black, ref best, ref bestDistance);
            return best;
        }

        private static void Consider(float ox, float min, float max, TeaVariety t, ref TeaVariety best, ref float bestDistance)
        {
            float d = ox < min ? min - ox : ox > max ? ox - max : 0f;
            if (d < bestDistance - 1e-6f || (Math.Abs(d - bestDistance) <= 1e-6f && t > best))
            {
                best = t;
                bestDistance = d;
            }
        }

        /// <summary>The oxidation that suits a tea best.</summary>
        public static float SweetSpot(TeaVariety t, VarietyParams v)
        {
            switch (t)
            {
                case TeaVariety.LightOolong: return v.lightOolongSweet;
                case TeaVariety.DarkOolong: return v.darkOolongSweet;
                case TeaVariety.Black: return v.blackSweet;
                default: return v.paleSweet;
            }
        }

        /// <summary>Catalogue ingredient id of the base tea.</summary>
        public static string IngredientId(TeaVariety t)
        {
            switch (t)
            {
                case TeaVariety.White: return TeaIds.White;
                case TeaVariety.Green: return TeaIds.Green;
                case TeaVariety.Black: return TeaIds.Black;
                default: return TeaIds.Oolong;
            }
        }

        /// <summary>
        /// Where the batch is heading if the player carries on the obvious way from here
        /// (the preview at every station): finish drying now, or – while oxidizing – stop now.
        /// </summary>
        public static TeaVariety Heading(LeafBatch b, ProcessingParams p) => Classify(b, p.variety);
    }

    /// <summary>Catalogue ids of the processed teas (added to the ingredient catalogue).</summary>
    public static class TeaIds
    {
        public const string White = "tea_white";
        public const string Green = "tea_green";
        public const string Oolong = "tea_oolong";
        public const string Black = "tea_black";

        /// <summary>Fresh leaves of the tea shrub's first harvest ("erster Austrieb").</summary>
        public const string FirstFlushLeaf = "tea_leaf_first";
    }
}
