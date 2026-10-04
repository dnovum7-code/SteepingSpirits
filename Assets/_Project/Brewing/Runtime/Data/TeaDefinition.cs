using UnityEngine;
using SteepingSpirits.Brewing.Core;

namespace SteepingSpirits.Brewing.Data
{
    /// <summary>One tea: simulation values (Core) plus how it looks.</summary>
    [CreateAssetMenu(fileName = "Tea_New", menuName = "SteepingSpirits/Brewing/Tea Definition")]
    public class TeaDefinition : ScriptableObject
    {
        public string displayName = "Tee";

        [TextArea(2, 4)]
        public string description;

        [Tooltip("Extraction behaviour – the numbers that make each tea feel different")]
        public TeaParams parameters = new TeaParams();

        [Tooltip("Liquor colour from clear (0) to fully extracted (1), driven by A / Amax")]
        public Gradient liquorColor = DefaultLiquor(new Color(0.75f, 0.4f, 0.15f));

        public Color leafColor = new Color(0.25f, 0.35f, 0.15f);

        public static Gradient DefaultLiquor(Color full)
        {
            var g = new Gradient();
            g.SetKeys(
                new[] { new GradientColorKey(new Color(0.9f, 0.95f, 1f), 0f), new GradientColorKey(full, 1f) },
                new[] { new GradientAlphaKey(0.25f, 0f), new GradientAlphaKey(0.9f, 1f) });
            return g;
        }
    }
}
