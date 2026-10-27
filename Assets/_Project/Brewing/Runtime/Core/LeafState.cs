namespace SteepingSpirits.Brewing.Core
{
    /// <summary>
    /// State of one portion of leaves. Phase 1 brews a single infusion, but the
    /// residual extract already scales the aroma ceiling so later infusions
    /// (Phase 2) can draw on what the leaves have left.
    /// </summary>
    public sealed class LeafState
    {
        public LeafState(float residualExtract = 1f)
        {
            ResidualExtract = residualExtract;
        }

        /// <summary>Share of extractable substance still in the leaves (1 = fresh).</summary>
        public float ResidualExtract { get; set; }
    }
}
