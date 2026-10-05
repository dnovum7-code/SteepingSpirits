using UnityEngine;

namespace SteepingSpirits.Platformer.JumpNRun
{
    /// <summary>Colour moods for levels ("@mood morning|evening|night" in the level file).</summary>
    public static class LevelMood
    {
        public struct Palette
        {
            public Color sky;
            public Color far;
            public Color mid;
            public Color near;
            public Color ground;
            public Color glow;
        }

        public static Palette For(string mood)
        {
            switch (mood)
            {
                case "evening":
                    return new Palette
                    {
                        sky = new Color(0.86f, 0.62f, 0.52f),
                        far = new Color(0.62f, 0.46f, 0.55f),
                        mid = new Color(0.46f, 0.36f, 0.46f),
                        near = new Color(0.32f, 0.28f, 0.34f),
                        ground = new Color(0.34f, 0.26f, 0.26f),
                        glow = new Color(1f, 0.72f, 0.45f, 0.10f)
                    };
                case "night":
                    return new Palette
                    {
                        sky = new Color(0.16f, 0.18f, 0.30f),
                        far = new Color(0.22f, 0.24f, 0.38f),
                        mid = new Color(0.18f, 0.20f, 0.30f),
                        near = new Color(0.13f, 0.15f, 0.22f),
                        ground = new Color(0.22f, 0.20f, 0.24f),
                        glow = new Color(0.6f, 0.7f, 1f, 0.06f)
                    };
                default:
                    return new Palette
                    {
                        sky = new Color(0.66f, 0.78f, 0.86f),
                        far = new Color(0.62f, 0.74f, 0.74f),
                        mid = new Color(0.50f, 0.66f, 0.56f),
                        near = new Color(0.40f, 0.56f, 0.42f),
                        ground = new Color(0.36f, 0.30f, 0.26f),
                        glow = new Color(1f, 0.95f, 0.8f, 0.05f)
                    };
            }
        }
    }
}
