using System.Collections.Generic;

namespace SteepingSpirits.Processing
{
    /// <summary>What the player has shown they can do (counted from finished batches; saved).</summary>
    public sealed class SpiritMastery
    {
        public int goodWithers;
        public int dried;
        public int goodWhites;

        public bool Wither(SpiritParams p) => goodWithers >= p.witherMasteredAfter;
        public bool Dry(SpiritParams p) => dried >= p.dryMasteredAfter;
        public bool White(SpiritParams p) => goodWhites >= p.whiteMasteredAfter && Wither(p);

        /// <summary>Counts a finished tea.</summary>
        public void Record(ProcessedTea tea, CareReport care, SpiritParams p)
        {
            if (tea == null) return;
            dried++;
            if (care.wither >= p.goodWitherCare) goodWithers++;
            if (tea.variety == TeaVariety.White && tea.tier >= 2) goodWhites++;
        }
    }

    public enum SpiritDeedKind
    {
        TookOffRack,
        MovedToDryer,
        TookOutOfDryer
    }

    public struct SpiritDeed
    {
        public int batchId;
        public SpiritDeedKind kind;
    }

    /// <summary>
    /// The workshop spirit: a small helper that takes over passive steps the player has mastered –
    /// leaves off the rack at the right moment, dry batches out of the basket before they roast,
    /// and (when the player hands a batch over) the white-tea way from the rack straight into the dryer.
    /// It never decides a tea for the player: active steps and the oxidation stop stay with them.
    /// </summary>
    public static class WorkshopSpirit
    {
        public static List<SpiritDeed> Tend(Workshop w, SpiritMastery m, ICollection<int> whiteIntent)
        {
            var deeds = new List<SpiritDeed>();
            ProcessingParams p = w.settings;
            float ideal = (p.wither.idealMin + p.wither.idealMax) * 0.5f;
            w.Sync();
            var snapshot = new List<LeafBatch>(w.Batches);
            foreach (LeafBatch b in snapshot)
            {
                if (b.place == BatchPlace.WitherRack && m.Wither(p.spirit) && b.moisture <= ideal)
                {
                    if (!w.Do(b.id, BatchAction.TakeOut)) continue;
                    if (whiteIntent != null && whiteIntent.Contains(b.id) && m.White(p.spirit) && w.Do(b.id, BatchAction.Dry))
                    {
                        whiteIntent.Remove(b.id);
                        deeds.Add(new SpiritDeed { batchId = b.id, kind = SpiritDeedKind.MovedToDryer });
                    }
                    else
                    {
                        deeds.Add(new SpiritDeed { batchId = b.id, kind = SpiritDeedKind.TookOffRack });
                    }
                }
                else if (b.place == BatchPlace.DryBasket && m.Dry(p.spirit) && b.IsDry(p.dry))
                {
                    if (w.Do(b.id, BatchAction.TakeOut)) deeds.Add(new SpiritDeed { batchId = b.id, kind = SpiritDeedKind.TookOutOfDryer });
                }
            }

            return deeds;
        }
    }
}
