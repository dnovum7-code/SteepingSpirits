using System;

namespace SteepingSpirits.Processing
{
    /// <summary>What the player can do with a batch.</summary>
    public enum BatchAction
    {
        /// <summary>Lay it on the withering rack.</summary>
        Wither,

        /// <summary>Roll it on the rolling table (active).</summary>
        Roll,

        /// <summary>Put it into the oxidation basket.</summary>
        Oxidize,

        /// <summary>Fix it in the wok (active).</summary>
        Fix,

        /// <summary>Put it into the drying basket over the embers.</summary>
        Dry,

        /// <summary>Take it from a rack or basket back to the shelf.</summary>
        TakeOut,

        /// <summary>The batch is dry: pack it as tea.</summary>
        Finish
    }

    /// <summary>Why an action is not possible right now (a calm hint, never an error).</summary>
    public enum BatchReason
    {
        Ok,
        NotOnShelf,
        AlreadyOnShelf,
        NotWithered,
        AlreadyRolled,
        RolledEnough,
        NotRolled,
        AlreadyFixed,
        TooDry,
        AlreadyDry,
        NotDry
    }

    public struct BatchCheck
    {
        public bool ok;
        public BatchReason reason;

        public static readonly BatchCheck Yes = new BatchCheck { ok = true, reason = BatchReason.Ok };

        public static BatchCheck No(BatchReason r) => new BatchCheck { ok = false, reason = r };

        public override string ToString() => ok ? "ok" : "no: " + reason;
    }

    /// <summary>Result of a rolling session.</summary>
    public struct RollOutcome
    {
        /// <summary>How much the leaves were rolled this time (0..1).</summary>
        public float intensity;

        /// <summary>How even the rhythm was (0..1).</summary>
        public float evenness;

        public float seconds;
    }

    /// <summary>Result of a wok session: shares of the time below, inside and above the heat window.</summary>
    public struct WokOutcome
    {
        public float under;
        public float inside;
        public float over;
        public float seconds;
    }

    /// <summary>
    /// All batch transitions as pure functions: they never change the given batch but return a
    /// changed copy. Nothing here can spoil a batch – every path still ends in a valid tea.
    /// </summary>
    public static class BatchRules
    {
        private const float WorkDrynessLimit = 0.3f;

        public static BatchCheck Check(LeafBatch b, BatchAction action, ProcessingParams p)
        {
            if (action == BatchAction.TakeOut) return b.place != BatchPlace.Shelf ? BatchCheck.Yes : BatchCheck.No(BatchReason.AlreadyOnShelf);
            if (b.place != BatchPlace.Shelf) return BatchCheck.No(BatchReason.NotOnShelf);

            switch (action)
            {
                case BatchAction.Wither:
                    if (b.IsDry(p.dry) || b.dryness >= WorkDrynessLimit) return BatchCheck.No(BatchReason.AlreadyDry);
                    if (b.fixedByWok) return BatchCheck.No(BatchReason.AlreadyFixed);
                    if (b.rollCount > 0) return BatchCheck.No(BatchReason.AlreadyRolled);
                    return BatchCheck.Yes;

                case BatchAction.Roll:
                    if (b.dryness >= WorkDrynessLimit) return BatchCheck.No(BatchReason.TooDry);
                    if (!b.IsWithered(p.wither)) return BatchCheck.No(BatchReason.NotWithered);
                    if (b.rollCount >= p.roll.maxRolls) return BatchCheck.No(BatchReason.RolledEnough);
                    return BatchCheck.Yes;

                case BatchAction.Oxidize:
                    if (b.dryness >= WorkDrynessLimit) return BatchCheck.No(BatchReason.TooDry);
                    if (b.fixedByWok) return BatchCheck.No(BatchReason.AlreadyFixed);
                    if (b.rollCount == 0) return BatchCheck.No(BatchReason.NotRolled);
                    return BatchCheck.Yes;

                case BatchAction.Fix:
                    if (b.dryness >= WorkDrynessLimit) return BatchCheck.No(BatchReason.TooDry);
                    if (b.fixedByWok) return BatchCheck.No(BatchReason.AlreadyFixed);
                    if (!b.IsWithered(p.wither)) return BatchCheck.No(BatchReason.NotWithered);
                    return BatchCheck.Yes;

                case BatchAction.Dry:
                    if (b.IsDry(p.dry)) return BatchCheck.No(BatchReason.AlreadyDry);
                    if (!b.IsWithered(p.wither)) return BatchCheck.No(BatchReason.NotWithered);
                    return BatchCheck.Yes;

                case BatchAction.Finish:
                    return b.IsDry(p.dry) ? BatchCheck.Yes : BatchCheck.No(BatchReason.NotDry);

                default:
                    return BatchCheck.No(BatchReason.NotOnShelf);
            }
        }

        /// <summary>Move to a rack or basket (Wither, Oxidize, Dry) or back to the shelf (TakeOut).</summary>
        public static LeafBatch Place(LeafBatch b, BatchAction action, ProcessingParams p)
        {
            BatchCheck c = Check(b, action, p);
            if (!c.ok) return b;
            LeafBatch n = b.Clone();
            switch (action)
            {
                case BatchAction.Wither:
                    n.place = BatchPlace.WitherRack;
                    break;
                case BatchAction.Oxidize:
                    MarkWork(n);
                    n.place = BatchPlace.OxidationBasket;
                    n.oxidationStopped = false;
                    break;
                case BatchAction.Dry:
                    MarkWork(n);
                    n.place = BatchPlace.DryBasket;
                    break;
                case BatchAction.TakeOut:
                    if (n.place == BatchPlace.WitherRack) AddStep(n, ProcessStep.Wither);
                    if (n.place == BatchPlace.OxidationBasket)
                    {
                        AddStep(n, ProcessStep.Oxidize);
                        n.oxidationStopped = true;
                        n.oxidationAtStop = n.oxidation;
                    }

                    if (n.place == BatchPlace.DryBasket && n.IsDry(p.dry)) AddStep(n, ProcessStep.Dry);
                    n.place = BatchPlace.Shelf;
                    break;
            }

            return n;
        }

        /// <summary>Applies a rolling session. Rolling bruises the cells and starts oxidation.</summary>
        public static LeafBatch Roll(LeafBatch b, RollOutcome r, ProcessingParams p)
        {
            if (!Check(b, BatchAction.Roll, p).ok) return b;
            LeafBatch n = b.Clone();
            MarkWork(n);
            float intensity = Clamp01(r.intensity);
            n.rollEvenness = (n.rollEvenness * n.rollCount + Clamp01(r.evenness)) / (n.rollCount + 1);
            n.rollCount++;
            n.rolled = Math.Max(n.rolled, intensity);
            if (!n.fixedByWok) n.oxidation = Clamp01(n.oxidation + p.roll.oxidationKick * intensity);
            n.moisture = Math.Max(0.05f, n.moisture - p.roll.moistureLoss * intensity);
            n.activeSeconds += Math.Max(0f, r.seconds);
            n.steps.Add(ProcessStep.Roll);
            return n;
        }

        /// <summary>Applies a wok session. Fixing stops oxidation; too little heat lets it creep on, too much roasts.</summary>
        public static LeafBatch Fix(LeafBatch b, WokOutcome w, ProcessingParams p)
        {
            if (!Check(b, BatchAction.Fix, p).ok) return b;
            LeafBatch n = b.Clone();
            MarkWork(n);
            float total = Math.Max(0.0001f, w.under + w.inside + w.over);
            float under = w.under / total, inside = w.inside / total, over = w.over / total;
            n.fixedByWok = true;
            n.wokCare = inside;
            n.fixResidual = under * p.oxidation.weakFixResidual;
            n.roast = Math.Min(1f, n.roast + over * p.wok.roastPerOverheat);
            n.moisture = Math.Max(0.05f, n.moisture - 0.05f);
            n.activeSeconds += Math.Max(0f, w.seconds);
            n.steps.Add(ProcessStep.Fix);
            return n;
        }

        /// <summary>Time passes (game minutes): withering, oxidation, drying, roasting.</summary>
        public static LeafBatch Elapse(LeafBatch b, double minutes, ProcessingParams p)
        {
            LeafBatch n = b.Clone();
            if (minutes <= 0) return n;
            n.lastMinutes = b.lastMinutes + minutes;
            double left = minutes;
            float stepMax = Math.Max(0.1f, p.integrationStep);
            while (left > 0)
            {
                float dt = (float)Math.Min(left, stepMax);
                left -= dt;
                Integrate(n, dt, p);
            }

            return n;
        }

        /// <summary>Brings the batch up to the given game time (nothing happens if time went backwards).</summary>
        public static LeafBatch AdvanceTo(LeafBatch b, double nowMinutes, ProcessingParams p)
        {
            if (nowMinutes <= b.lastMinutes)
            {
                if (nowMinutes >= b.lastMinutes) return b;
                LeafBatch rebased = b.Clone();
                rebased.lastMinutes = nowMinutes; // the clock was reset: no time passes
                return rebased;
            }

            return Elapse(b, nowMinutes - b.lastMinutes, p);
        }

        private static void Integrate(LeafBatch n, float dt, ProcessingParams p)
        {
            bool dryNow = n.place == BatchPlace.DryBasket;

            // Water: withering on the rack, slow loss elsewhere, drying in the basket.
            if (dryNow)
            {
                n.dryness = Math.Min(1.2f, n.dryness + dt / Math.Max(1f, p.dry.minutesToDry));
            }
            else if (n.dryness <= 0f)
            {
                float factor = n.place == BatchPlace.WitherRack ? 1f : p.wither.offRackFactor;
                float k = (float)Math.Exp(-dt * factor / Math.Max(1f, p.wither.tauMinutes));
                n.moisture = p.wither.floor + (n.moisture - p.wither.floor) * k;
            }

            if (n.dryness > 0f) n.moisture = Math.Min(n.moisture, Math.Max(0.03f, 0.6f * (1f - Math.Min(1f, n.dryness))));

            // Oxidation.
            OxidationParams o = p.oxidation;
            float rate = o.ratePerMinute * (n.rollCount > 0 ? o.rollIndependentShare + (1f - o.rollIndependentShare) * n.rolled : o.unrolledFactor);
            if (n.fixedByWok) rate *= n.fixResidual;
            else if (n.rollCount > 0 && n.oxidationStopped && n.place != BatchPlace.OxidationBasket) rate *= o.setAsideFactor;
            rate *= Math.Max(0f, 1f - Math.Min(1f, n.dryness) / Math.Max(0.01f, o.dryStopsAt));
            n.oxidation = Clamp01(n.oxidation + rate * dt);

            // Over-drying gives a light roast.
            if (dryNow && n.dryness >= 1f)
            {
                float room = Math.Max(0f, p.dry.overDryRoastCap - n.roastFromDrying);
                float add = Math.Min(room, p.dry.overDryRoastPerMinute * dt);
                n.roastFromDrying += add;
                n.roast = Math.Min(1f, n.roast + add);
            }
        }

        private static void MarkWork(LeafBatch n)
        {
            if (n.workMoisture < 0f) n.workMoisture = n.moisture;
        }

        private static void AddStep(LeafBatch n, ProcessStep s)
        {
            if (n.steps.Count == 0 || n.steps[n.steps.Count - 1] != s) n.steps.Add(s);
        }

        private static float Clamp01(float v) => v < 0f ? 0f : v > 1f ? 1f : v;
    }
}
