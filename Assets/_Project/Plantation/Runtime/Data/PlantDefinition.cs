using UnityEngine;

namespace SteepingSpirits.Plantation
{
    /// <summary>
    /// One garden plant: growth data (ingredient, seed, stages, water need, regrowth, yield) plus its
    /// look. Empty sprite slots use the soft placeholder shapes, so art can be dropped in stage by stage.
    /// Created by SteepingSpirits → Plantation → Build Garden; tuned here in the Inspector.
    /// </summary>
    [CreateAssetMenu(menuName = "SteepingSpirits/Plantation/Plant Definition", fileName = "Plant")]
    public class PlantDefinition : ScriptableObject
    {
        public PlantParams plant = new PlantParams();

        [Header("Look")]
        [Tooltip("Optional sprite per stage (index = stage, last = ripe). Empty = placeholder shape")]
        public Sprite[] stageSprites = new Sprite[0];

        [Tooltip("Placeholder colour of the growing plant")]
        public Color leafColor = new Color(0.40f, 0.68f, 0.36f);

        [Tooltip("Placeholder colour of what is harvested (blossom or fresh leaves)")]
        public Color ripeColor = new Color(0.62f, 0.86f, 0.42f);

        /// <summary>Sprite for a stage, or null for the placeholder.</summary>
        public Sprite SpriteFor(int stage)
        {
            if (stageSprites == null || stage < 0 || stage >= stageSprites.Length) return null;
            return stageSprites[stage];
        }
    }

    /// <summary>Default placeholder colours of the starter plants (builder and fallback).</summary>
    public static class PlantPalette
    {
        public static Color Leaf(string plantId)
        {
            switch (plantId)
            {
                case StarterPlants.Tea: return new Color(0.27f, 0.52f, 0.28f);
                case StarterPlants.Mint: return new Color(0.38f, 0.74f, 0.52f);
                case StarterPlants.Chamomile: return new Color(0.50f, 0.70f, 0.38f);
                case StarterPlants.Jasmine: return new Color(0.30f, 0.56f, 0.34f);
                case StarterPlants.Lavender: return new Color(0.48f, 0.62f, 0.50f);
                case StarterPlants.LemonBalm: return new Color(0.55f, 0.76f, 0.34f);
                default: return new Color(0.40f, 0.68f, 0.36f);
            }
        }

        public static Color Ripe(string plantId)
        {
            switch (plantId)
            {
                case StarterPlants.Tea: return new Color(0.58f, 0.84f, 0.40f);
                case StarterPlants.Mint: return new Color(0.66f, 0.95f, 0.74f);
                case StarterPlants.Chamomile: return new Color(1.00f, 0.95f, 0.62f);
                case StarterPlants.Jasmine: return new Color(1.00f, 0.98f, 0.90f);
                case StarterPlants.Lavender: return new Color(0.72f, 0.58f, 0.92f);
                case StarterPlants.LemonBalm: return new Color(0.88f, 0.94f, 0.45f);
                default: return new Color(0.62f, 0.86f, 0.42f);
            }
        }
    }
}
