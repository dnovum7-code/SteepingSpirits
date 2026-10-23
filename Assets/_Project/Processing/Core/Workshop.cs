using System;
using System.Collections.Generic;

namespace SteepingSpirits.Processing
{
    /// <summary>
    /// All batches in work and the clock they follow. Passive steps run on game time: every call
    /// of <see cref="Sync"/> brings each batch up to "now" – also after the player was away.
    /// </summary>
    public sealed class Workshop
    {
        public readonly ProcessingParams settings;
        private readonly IGameTime time;
        private readonly List<LeafBatch> batches = new List<LeafBatch>();

        public int NextId { get; set; } = 1;

        public IReadOnlyList<LeafBatch> Batches => batches;

        public double Now => time.Minutes;

        public Workshop(ProcessingParams settings, IGameTime time)
        {
            this.settings = settings ?? new ProcessingParams();
            this.time = time ?? new ManualGameTime();
        }

        /// <summary>Starts a new batch of fresh leaves on the shelf.</summary>
        public LeafBatch Add(string sourceId, int originTier, int amount, bool firstFlush)
        {
            LeafBatch b = LeafBatch.Fresh(NextId++, sourceId, originTier, amount, firstFlush, Now);
            batches.Add(b);
            return b;
        }

        /// <summary>Puts back a saved batch (keeps its own last time, so the time away is caught up).</summary>
        public void Restore(LeafBatch b)
        {
            if (b == null) return;
            batches.Add(b.Clone());
            NextId = Math.Max(NextId, b.id + 1);
        }

        public LeafBatch Get(int id)
        {
            foreach (LeafBatch b in batches)
            {
                if (b.id == id) return b;
            }

            return null;
        }

        /// <summary>Brings every batch up to the current game time.</summary>
        public void Sync()
        {
            double now = Now;
            for (int i = 0; i < batches.Count; i++) batches[i] = BatchRules.AdvanceTo(batches[i], now, settings);
        }

        public BatchCheck Check(int id, BatchAction action)
        {
            LeafBatch b = Get(id);
            return b == null ? BatchCheck.No(BatchReason.NotOnShelf) : BatchRules.Check(b, action, settings);
        }

        /// <summary>Places, takes out (rack/basket actions). Returns false if not allowed.</summary>
        public bool Do(int id, BatchAction action)
        {
            Sync();
            return Replace(id, b => BatchRules.Place(b, action, settings));
        }

        public bool Roll(int id, RollOutcome r)
        {
            Sync();
            return Replace(id, b => BatchRules.Roll(b, r, settings));
        }

        public bool Fix(int id, WokOutcome w)
        {
            Sync();
            return Replace(id, b => BatchRules.Fix(b, w, settings));
        }

        /// <summary>Real seconds the player spent watching (oxidation basket) – telemetry only.</summary>
        public void AddActiveSeconds(int id, float seconds)
        {
            Replace(id, b =>
            {
                LeafBatch n = b.Clone();
                n.activeSeconds += Math.Max(0f, seconds);
                return n;
            });
        }

        /// <summary>Packs a dry batch as tea and removes it from the workshop.</summary>
        public bool Finish(int id, out ProcessedTea tea)
        {
            Sync();
            tea = null;
            LeafBatch b = Get(id);
            if (b == null || !BatchRules.Check(b, BatchAction.Finish, settings).ok) return false;
            tea = ProcessedTea.From(b, settings);
            batches.Remove(b);
            return true;
        }

        private bool Replace(int id, Func<LeafBatch, LeafBatch> change)
        {
            for (int i = 0; i < batches.Count; i++)
            {
                if (batches[i].id != id) continue;
                LeafBatch before = batches[i];
                LeafBatch after = change(before);
                batches[i] = after;
                return !ReferenceEquals(before, after);
            }

            return false;
        }
    }
}
