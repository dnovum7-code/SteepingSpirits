namespace SteepingSpirits.Brewing.Core
{
    /// <summary>The character of one infusion – a story layer for later (guests, memories).</summary>
    public enum InfusionCharacter
    {
        Faint,   // little aroma left
        Bright,  // fresh, aromatic, barely bitter
        Robust,  // strong with noticeable bitterness
        Mellow   // soft and rounded, typical for later infusions
    }

    /// <summary>One layer in the history of a portion of leaves.</summary>
    public sealed class InfusionProfile
    {
        public int InfusionIndex { get; internal set; }
        public float Aroma { get; internal set; }
        public float Bitterness { get; internal set; }
        public float Harmony { get; internal set; }
        public QualityTier Tier { get; internal set; }
        public bool MemoryCaught { get; internal set; }
        public InfusionCharacter Character { get; internal set; }

        /// <summary>Simple rule, tuned via QualityParams – deterministic and testable.</summary>
        public static InfusionCharacter Classify(int infusionIndex, float aroma, float bitterness, float bitterTolerance,
            QualityParams quality)
        {
            if (aroma < quality.faintAromaThreshold) return InfusionCharacter.Faint;
            if (bitterness > bitterTolerance * quality.robustBitterShare) return InfusionCharacter.Robust;
            return infusionIndex == 0 ? InfusionCharacter.Bright : InfusionCharacter.Mellow;
        }
    }
}
