using System;
using System.Collections.Generic;

namespace SteepingSpirits.Processing
{
    /// <summary>Why a batch is not (yet) maocha for a pu-erh brick.</summary>
    public enum MaochaReason
    {
        Ok,
        NotDry,
        NotFixed,
        FixedTooHot,
        NotRolledEnough,
        TooOxidized,
        IsHerb
    }

    /// <summary>
    /// A pressed tea brick – prepared for pu-erh. Maturation is not simulated yet; the brick only
    /// remembers when it was pressed so a later system can let it age (maturationDays stays 0).
    /// </summary>
    [Serializable]
    public sealed class TeaBrick
    {
        public string sourceId = "";
        public int tier = 1;
        public List<TeaNote> notes = new List<TeaNote>();
        public float oxidation;
        public int portions;
        public double pressedAtMinutes;
        public int maturationDays;

        /// <summary>Id for a later catalogue entry (not added yet): tea_brick:raw/notes+qN.</summary>
        public string Id
        {
            get
            {
                string notePart = "";
                for (int i = 0; i < notes.Count; i++) notePart += (i == 0 ? "/" : ".") + TeaNotes.Key(notes[i]);
                return SteepingSpirits.Ingredients.IngredientQuality.With("tea_brick:raw" + notePart, tier);
            }
        }
    }

    /// <summary>
    /// Pu-erh preparation path: wither → fix gently in the wok (a little too cool on purpose, so
    /// the leaves can keep changing as they age) → roll firmly → dry = maocha → press into a brick.
    /// </summary>
    public static class PuErh
    {
        public static MaochaReason CheckMaocha(LeafBatch b, ProcessingParams p)
        {
            if (b.isHerb) return MaochaReason.IsHerb;
            if (!b.IsDry(p.dry)) return MaochaReason.NotDry;
            if (!b.fixedByWok) return MaochaReason.NotFixed;
            if (b.fixResidual < p.puerh.minFixResidual) return MaochaReason.FixedTooHot;
            if (b.rolled < p.puerh.minRolled) return MaochaReason.NotRolledEnough;
            if (b.oxidation > p.puerh.maxOxidation) return MaochaReason.TooOxidized;
            return MaochaReason.Ok;
        }

        public static bool IsMaocha(LeafBatch b, ProcessingParams p) => CheckMaocha(b, p) == MaochaReason.Ok;

        /// <summary>Presses maocha into bricks (one per portionsPerBrick portions, at least one).</summary>
        public static bool TryPress(LeafBatch b, ProcessingParams p, double nowMinutes, out TeaBrick brick)
        {
            brick = null;
            if (!IsMaocha(b, p)) return false;
            brick = new TeaBrick
            {
                sourceId = b.sourceId,
                tier = TeaQuality.Tier(b, p),
                notes = TeaNotes.For(b, p),
                oxidation = b.oxidation,
                portions = Math.Max(1, b.amount),
                pressedAtMinutes = nowMinutes,
                maturationDays = 0
            };
            return true;
        }
    }
}
