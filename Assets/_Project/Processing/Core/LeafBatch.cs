using System;
using System.Collections.Generic;
using System.Text;

namespace SteepingSpirits.Processing
{
    /// <summary>The processing steps a batch can go through.</summary>
    public enum ProcessStep
    {
        Wither,
        Roll,
        Oxidize,
        Fix,
        Dry
    }

    /// <summary>Where a batch lies.</summary>
    public enum BatchPlace
    {
        /// <summary>The shelf for batches in work (nothing happens there except slow natural changes).</summary>
        Shelf,
        WitherRack,
        OxidationBasket,
        DryBasket
    }

    /// <summary>
    /// One batch of tea leaves on its way to tea. All values 0..1. Changes only through the pure
    /// functions in <see cref="BatchRules"/>, which return a changed copy.
    /// </summary>
    [Serializable]
    public sealed class LeafBatch
    {
        public int id;

        /// <summary>Leaves in the batch (= tea portions at the end).</summary>
        public int amount = 1;

        /// <summary>Plain ingredient id of the leaves (tea_leaf, tea_leaf_first, golden_tip).</summary>
        public string sourceId = "tea_leaf";

        /// <summary>Garden quality tier of the leaves (1 gut, 2 fein, 3 erlesen).</summary>
        public int originTier = 1;

        /// <summary>First flush ("erster Austrieb") or golden tips: lifts the quality ceiling.</summary>
        public bool firstFlush;

        /// <summary>A herb or blossom from the garden: it can only be dried (becomes "&lt;herb&gt;_dried").</summary>
        public bool isHerb;

        public float moisture = 1f;

        /// <summary>Strongest roll so far.</summary>
        public float rolled;

        /// <summary>Average evenness of all rolls (care).</summary>
        public float rollEvenness;

        public int rollCount;

        public float oxidation;

        /// <summary>Taken out of the basket: rolled leaves set aside oxidize much slower.</summary>
        public bool oxidationStopped;

        /// <summary>Oxidation at the moment the player stopped it (telemetry "target").</summary>
        public float oxidationAtStop = -1f;

        public bool fixedByWok;

        /// <summary>Share of the wok time spent inside the heat window (care).</summary>
        public float wokCare;

        /// <summary>Remaining oxidation after a weak fix (relative to the full rate).</summary>
        public float fixResidual;

        public float roast;

        /// <summary>Part of the roast that came from over-drying (capped separately).</summary>
        public float roastFromDrying;

        public float dryness;

        /// <summary>Moisture when the leaves were first worked on after withering (-1 = not yet).</summary>
        public float workMoisture = -1f;

        public BatchPlace place = BatchPlace.Shelf;

        /// <summary>Game time (minutes) up to which passive changes are applied.</summary>
        public double lastMinutes;

        /// <summary>Real seconds spent in active steps (rolling, wok, watching the basket).</summary>
        public float activeSeconds;

        public List<ProcessStep> steps = new List<ProcessStep>();

        public bool IsWithered(WitherParams p) => moisture <= p.witheredMax;

        public bool IsDry(DryParams p) => dryness >= p.doneAt;

        public LeafBatch Clone()
        {
            var b = (LeafBatch)MemberwiseClone();
            b.steps = new List<ProcessStep>(steps);
            return b;
        }

        /// <summary>Path as short letters, e.g. "W-R-O-F-R-D".</summary>
        public string PathCode()
        {
            var sb = new StringBuilder();
            foreach (ProcessStep s in steps)
            {
                if (sb.Length > 0) sb.Append('-');
                sb.Append(s.ToString()[0]);
            }

            return sb.ToString();
        }

        public static LeafBatch Herb(int id, string herbId, int originTier, int amount, double nowMinutes)
        {
            LeafBatch b = Fresh(id, herbId, originTier, amount, false, nowMinutes);
            b.isHerb = true;
            return b;
        }

        public static LeafBatch Fresh(int id, string sourceId, int originTier, int amount, bool firstFlush, double nowMinutes) => new LeafBatch
        {
            id = id,
            sourceId = sourceId,
            originTier = Math.Max(1, Math.Min(3, originTier)),
            amount = Math.Max(1, amount),
            firstFlush = firstFlush,
            lastMinutes = nowMinutes
        };
    }
}
