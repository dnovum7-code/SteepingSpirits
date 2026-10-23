using UnityEngine;

namespace SteepingSpirits.Processing
{
    /// <summary>
    /// How a batch looks: colour from green over copper to dark brown (oxidation), duller and darker
    /// when dry or roasted; shape from flat to rolled; gloss while moist and freshly rolled.
    /// </summary>
    public static class LeafLook
    {
        private static readonly Color Fresh = new Color(0.36f, 0.66f, 0.30f);
        private static readonly Color Copper = new Color(0.72f, 0.46f, 0.20f);
        private static readonly Color Brown = new Color(0.33f, 0.20f, 0.11f);

        public static Color Colour(LeafBatch b)
        {
            float ox = Mathf.Clamp01(b.oxidation);
            Color c = ox < 0.45f ? Color.Lerp(Fresh, Copper, ox / 0.45f) : Color.Lerp(Copper, Brown, (ox - 0.45f) / 0.55f);
            float dry = Mathf.Clamp01(b.dryness);
            c = Color.Lerp(c, c * 0.7f + new Color(0.08f, 0.08f, 0.06f), dry * 0.6f);
            c = Color.Lerp(c, new Color(0.25f, 0.15f, 0.08f), Mathf.Clamp01(b.roast) * 0.5f);
            if (b.moisture > 0.85f && b.dryness <= 0f) c = Color.Lerp(c, new Color(0.45f, 0.78f, 0.38f), (b.moisture - 0.85f) * 3f);
            c.a = 1f;
            return c;
        }

        /// <summary>Width/height of one leaf: flat (wide) to rolled (small, round); a little shrink when withered.</summary>
        public static Vector2 Shape(LeafBatch b)
        {
            float rolled = Mathf.Clamp01(b.rolled);
            float withered = Mathf.InverseLerp(1f, 0.6f, b.moisture);
            float w = Mathf.Lerp(0.34f, 0.16f, rolled) * Mathf.Lerp(1f, 0.88f, withered);
            float h = Mathf.Lerp(0.16f, 0.15f, rolled) * Mathf.Lerp(1f, 0.92f, withered);
            if (b.dryness > 0f) { w *= 1f - 0.1f * Mathf.Clamp01(b.dryness); h *= 1f - 0.1f * Mathf.Clamp01(b.dryness); }
            return new Vector2(w, h);
        }

        /// <summary>Sheen 0..1: fresh moist leaves and freshly rolled ones glisten, dry ones are matt.</summary>
        public static float Gloss(LeafBatch b)
        {
            if (b.dryness > 0.5f) return 0.05f + 0.15f * Mathf.Clamp01(b.roast);
            return Mathf.Clamp01(b.moisture - 0.55f) * 0.6f + Mathf.Clamp01(b.rolled) * 0.25f;
        }

        public static Color TeaColor(TeaVariety v)
        {
            switch (v)
            {
                case TeaVariety.White: return new Color(0.78f, 0.80f, 0.70f);
                case TeaVariety.Green: return new Color(0.35f, 0.55f, 0.28f);
                case TeaVariety.LightOolong: return new Color(0.48f, 0.52f, 0.28f);
                case TeaVariety.DarkOolong: return new Color(0.50f, 0.32f, 0.16f);
                default: return new Color(0.28f, 0.17f, 0.10f);
            }
        }
    }
}
