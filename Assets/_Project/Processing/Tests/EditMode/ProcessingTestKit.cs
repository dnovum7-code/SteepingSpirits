namespace SteepingSpirits.Processing.Tests
{
    internal static class Kit
    {
        public static readonly ProcessingParams P = new ProcessingParams();

        public static LeafBatch Fresh(int tier = 3, bool firstFlush = false) => LeafBatch.Fresh(1, "tea_leaf", tier, 5, firstFlush, 0);

        public static LeafBatch Withered(int tier = 3, double minutes = 240, bool firstFlush = false)
        {
            LeafBatch b = BatchRules.Place(Fresh(tier, firstFlush), BatchAction.Wither, P);
            b = BatchRules.Elapse(b, minutes, P);
            return BatchRules.Place(b, BatchAction.TakeOut, P);
        }

        public static RollOutcome Roll(float intensity, float evenness = 1f) => new RollOutcome { intensity = intensity, evenness = evenness, seconds = 12f };

        public static WokOutcome Wok(float under, float inside, float over) => new WokOutcome { under = under, inside = inside, over = over, seconds = 12f };

        /// <summary>Put into the oxidation basket until the oxidation reaches the target, then take out.</summary>
        public static LeafBatch OxidizeTo(LeafBatch b, float target)
        {
            b = BatchRules.Place(b, BatchAction.Oxidize, P);
            int guard = 0;
            while (b.oxidation < target && guard++ < 2000) b = BatchRules.Elapse(b, 1, P);
            return BatchRules.Place(b, BatchAction.TakeOut, P);
        }

        public static LeafBatch DryFully(LeafBatch b, double extraMinutes = 0)
        {
            b = BatchRules.Place(b, BatchAction.Dry, P);
            b = BatchRules.Elapse(b, P.dry.minutesToDry + extraMinutes, P);
            return BatchRules.Place(b, BatchAction.TakeOut, P);
        }

        public static LeafBatch White(int tier = 3, double witherMinutes = 240) => DryFully(Withered(tier, witherMinutes));

        public static LeafBatch Green(int tier = 3)
        {
            LeafBatch b = BatchRules.Fix(Withered(tier), Wok(0f, 1f, 0f), P);
            b = BatchRules.Roll(b, Roll(1f), P);
            return DryFully(b);
        }

        public static LeafBatch Oolong(float target, int tier = 3)
        {
            LeafBatch b = BatchRules.Roll(Withered(tier), Roll(0.5f), P);
            b = OxidizeTo(b, target);
            b = BatchRules.Fix(b, Wok(0f, 1f, 0f), P);
            b = BatchRules.Roll(b, Roll(1f), P);
            return DryFully(b);
        }

        public static LeafBatch Black(int tier = 3, float target = 0.92f)
        {
            LeafBatch b = BatchRules.Roll(Withered(tier), Roll(1f), P);
            b = OxidizeTo(b, target);
            return DryFully(b);
        }
    }
}
