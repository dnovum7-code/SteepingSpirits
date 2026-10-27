using System;
using System.Collections.Generic;
using System.Text;

namespace SteepingSpirits.Processing
{
    public enum WorkshopBot
    {
        /// <summary>Withers to the window, rolls on the beat, stops at the sweet spot, watches the wok.</summary>
        Careful,

        /// <summary>Guesses times, rolls unevenly, stops late, reacts slowly in the wok, forgets the dryer.</summary>
        Sloppy,

        /// <summary>Any allowed step in any order with random waits and random input.</summary>
        Random
    }

    /// <summary>Numbers of one simulated run.</summary>
    public sealed class WorkshopSimResult
    {
        public WorkshopBot bot;
        public int batches;
        public int hits;
        public readonly int[] byVariety = new int[5];
        public readonly int[] byTier = new int[4];
        public readonly int[] byNote = new int[6];
        public int invalid;

        /// <summary>Active seconds summed per wanted tea (and counts) – for the duration check.</summary>
        public readonly float[] activeByWanted = new float[5];
        public readonly int[] countByWanted = new int[5];
        public readonly float[] maxActiveByWanted = new float[5];

        /// <summary>Leaf origin × tier: origin 0..2 = garden tier gut/fein/erlesen, 3 = first flush (premium).</summary>
        public readonly int[,] byOriginTier = new int[4, 4];

        /// <summary>Resulting variety × tier.</summary>
        public readonly int[,] byVarietyTier = new int[5, 4];

        /// <summary>Workshop care (0..1) summed per resulting variety, and per step (wither, roll, oxidation, wok).</summary>
        public readonly float[] careByVariety = new float[5];
        public readonly float[] stepCare = new float[4];
        public readonly int[] stepCount = new int[4];

        /// <summary>Teas that hit the "erlesen" score but were held at the ceiling of ordinary leaves.</summary>
        public int heldByCeiling;

        /// <summary>Tiers if only first flush/golden tip lifted the ceiling (garden "erlesen" capped too) – for comparison.</summary>
        public readonly int[] byTierPremiumOnly = new int[4];

        public float HitRate => batches == 0 ? 0f : hits / (float)batches;

        public static readonly string[] OriginNames = { "gut", "fein", "erlesen", "Austrieb" };
        public static readonly string[] StepNames = { "welken", "rollen", "oxid.", "wok" };

        public float AverageCare(TeaVariety v)
        {
            int n = byVarietyTier[(int)v, 1] + byVarietyTier[(int)v, 2] + byVarietyTier[(int)v, 3];
            return n == 0 ? 0f : careByVariety[(int)v] / n;
        }

        public float AverageStepCare(int step) => stepCount[step] == 0 ? -1f : stepCare[step] / stepCount[step];

        /// <summary>Tiers per leaf origin: "gut 3/12/0 · fein …" (gut/fein/erlesen counts).</summary>
        public string OriginLine()
        {
            var sb = new StringBuilder($"{bot,-8} by origin (gut/fein/erlesen)");
            for (int o = 0; o < 4; o++)
            {
                if (byOriginTier[o, 1] + byOriginTier[o, 2] + byOriginTier[o, 3] == 0) continue;
                sb.Append($"  {OriginNames[o]} {byOriginTier[o, 1]}/{byOriginTier[o, 2]}/{byOriginTier[o, 3]}");
            }

            return sb.Append($"  held by ceiling {heldByCeiling}").ToString();
        }

        /// <summary>Tiers and average workshop care per resulting variety, and the average care per step.</summary>
        public string VarietyLine()
        {
            var sb = new StringBuilder($"{bot,-8} by tea (gut/fein/erlesen, care)");
            foreach (TeaVariety v in (TeaVariety[])Enum.GetValues(typeof(TeaVariety)))
            {
                int i = (int)v;
                if (byVariety[i] == 0) continue;
                sb.Append($"  {v} {byVarietyTier[i, 1]}/{byVarietyTier[i, 2]}/{byVarietyTier[i, 3]} c{AverageCare(v):0.00}");
            }

            sb.Append("  steps");
            for (int k = 0; k < 4; k++) sb.Append($" {StepNames[k]} {AverageStepCare(k):0.00}/{stepCount[k]}");
            return sb.ToString();
        }

        public float AverageTier
        {
            get
            {
                int n = byTier[1] + byTier[2] + byTier[3];
                return n == 0 ? 0f : (byTier[1] + 2f * byTier[2] + 3f * byTier[3]) / n;
            }
        }

        public float AverageActive(TeaVariety wanted) =>
            countByWanted[(int)wanted] == 0 ? 0f : activeByWanted[(int)wanted] / countByWanted[(int)wanted];

        public override string ToString()
        {
            var sb = new StringBuilder();
            sb.Append($"{bot,-8} hit {HitRate * 100f,3:0}%  tiers gut/fein/erlesen {byTier[1]}/{byTier[2]}/{byTier[3]} (avg {AverageTier:0.00})");
            sb.Append($"  teas W/G/lO/dO/B {byVariety[0]}/{byVariety[1]}/{byVariety[2]}/{byVariety[3]}/{byVariety[4]}");
            sb.Append("  notes");
            foreach (TeaNote n in (TeaNote[])Enum.GetValues(typeof(TeaNote))) sb.Append(' ').Append(TeaNotes.Key(n)).Append(' ').Append(byNote[(int)n]);
            sb.Append($"  invalid {invalid}");
            return sb.ToString();
        }

        public string DurationLine()
        {
            var sb = new StringBuilder($"{bot,-8} active seconds (avg/max)");
            foreach (TeaVariety v in (TeaVariety[])Enum.GetValues(typeof(TeaVariety)))
            {
                sb.Append($"  {v} {AverageActive(v):0}/{maxActiveByWanted[(int)v]:0}");
            }

            return sb.ToString();
        }
    }

    /// <summary>
    /// Processes batches with bots through the same rules and minigame models the game uses,
    /// to check tuning: careful hits the wanted tea and the best quality, sloppy always still gets a
    /// usable tea, and how long the active steps take.
    /// </summary>
    public static class WorkshopSimulator
    {
        /// <summary>Seconds one station interaction costs (lay on, take off, pack).</summary>
        public const float InteractionSeconds = 1f;

        /// <summary>
        /// Mixed leaves like a real garden: 50 % erlesen / 30 % fein / 20 % gut, 5 % first flush.
        /// <paramref name="fixedOrigin"/> 0..2 forces garden tier gut/fein/erlesen, 3 the first flush (same random
        /// sequence otherwise, so bots and wishes stay comparable).
        /// </summary>
        public static WorkshopSimResult Run(ProcessingParams p, WorkshopBot bot, int batches, int seed = 1, int fixedOrigin = -1)
        {
            var rng = new Random(seed * 7919 + (int)bot);
            var result = new WorkshopSimResult { bot = bot, batches = batches };
            for (int i = 0; i < batches; i++)
            {
                var wanted = (TeaVariety)rng.Next(5);
                double roll = rng.NextDouble();
                int tier = roll < 0.5 ? 3 : roll < 0.8 ? 2 : 1;
                bool first = rng.NextDouble() < 0.05;
                if (fixedOrigin >= 0)
                {
                    first = fixedOrigin == 3;
                    tier = first ? 3 : fixedOrigin + 1;
                }

                LeafBatch b = LeafBatch.Fresh(i + 1, first ? TeaIds.FirstFlushLeaf : "tea_leaf", tier, 4, first, 0);
                float interactions = 0f;

                b = bot == WorkshopBot.Random ? RandomPath(b, p, rng, ref interactions) : Path(b, wanted, bot == WorkshopBot.Careful, p, rng, ref interactions);
                if (!BatchRules.Check(b, BatchAction.Finish, p).ok)
                {
                    result.invalid++;
                    continue;
                }

                ProcessedTea tea = ProcessedTea.From(b, p);
                result.byVariety[(int)tea.variety]++;
                result.byTier[tea.tier]++;
                CareReport care = TeaQuality.Evaluate(b, p);
                result.byOriginTier[first ? 3 : tier - 1, tea.tier]++;
                result.byVarietyTier[(int)tea.variety, tea.tier]++;
                result.careByVariety[(int)tea.variety] += care.care;
                if (care.score >= p.quality.excellentScore && care.tier < 3) result.heldByCeiling++;
                result.byTierPremiumOnly[first ? care.tier : Math.Min(care.tier, Math.Max(1, p.quality.ordinaryCeiling))]++;
                float[] steps = { care.wither, care.roll, care.oxidation, care.wok };
                for (int k = 0; k < 4; k++)
                {
                    if (steps[k] < 0f) continue;
                    result.stepCare[k] += steps[k];
                    result.stepCount[k]++;
                }

                foreach (TeaNote n in tea.notes) result.byNote[(int)n]++;
                if (tea.variety == wanted) result.hits++;
                float active = b.activeSeconds + interactions + InteractionSeconds; // + packing
                result.activeByWanted[(int)wanted] += active;
                result.countByWanted[(int)wanted]++;
                result.maxActiveByWanted[(int)wanted] = Math.Max(result.maxActiveByWanted[(int)wanted], active);
            }

            return result;
        }

        private static LeafBatch Path(LeafBatch b, TeaVariety wanted, bool careful, ProcessingParams p, Random rng, ref float interactions)
        {
            // Wither.
            b = Act(b, BatchAction.Wither, p, ref interactions);
            if (careful)
            {
                while (b.moisture > (p.wither.idealMin + p.wither.idealMax) * 0.5f) b = BatchRules.Elapse(b, 5, p);
            }
            else
            {
                b = BatchRules.Elapse(b, 120 + rng.Next(0, 480), p);
            }

            b = Act(b, BatchAction.TakeOut, p, ref interactions);
            b = Wait(b, careful, rng, p);

            switch (wanted)
            {
                case TeaVariety.White:
                    break;
                case TeaVariety.Green:
                    b = Wok(b, careful, rng, p);
                    b = Roll(b, 1f, careful, rng, p);
                    break;
                case TeaVariety.Black:
                    b = Roll(b, 1f, careful, rng, p);
                    b = Oxidize(b, TeaClassifier.SweetSpot(wanted, p.variety), careful, rng, p, ref interactions);
                    break;
                default:
                    b = Roll(b, 0.5f, careful, rng, p);
                    b = Oxidize(b, TeaClassifier.SweetSpot(wanted, p.variety), careful, rng, p, ref interactions);
                    b = Wait(b, careful, rng, p);
                    b = Wok(b, careful, rng, p);
                    b = Roll(b, 1f, careful, rng, p);
                    break;
            }

            b = Wait(b, careful, rng, p);
            b = Act(b, BatchAction.Dry, p, ref interactions);
            b = BatchRules.Elapse(b, careful ? p.dry.minutesToDry * p.dry.doneAt + 2 : p.dry.minutesToDry + rng.Next(0, 300), p);
            return Act(b, BatchAction.TakeOut, p, ref interactions);
        }

        private static LeafBatch Act(LeafBatch b, BatchAction a, ProcessingParams p, ref float interactions)
        {
            LeafBatch n = BatchRules.Place(b, a, p);
            if (!ReferenceEquals(n, b)) interactions += InteractionSeconds;
            return n;
        }

        /// <summary>Sloppy players leave the batch lying around between steps.</summary>
        private static LeafBatch Wait(LeafBatch b, bool careful, Random rng, ProcessingParams p) =>
            careful ? BatchRules.Elapse(b, 1, p) : BatchRules.Elapse(b, rng.Next(0, 90), p);

        private static LeafBatch Roll(LeafBatch b, float wantIntensity, bool careful, Random rng, ProcessingParams p)
        {
            var s = new RollSession(p.roll);
            int kneads = (int)Math.Round(wantIntensity * p.roll.kneadsForFullRoll) + (careful ? 0 : rng.Next(-2, 3));
            for (int k = 0; k < Math.Max(1, kneads) && !s.Done; k++)
            {
                if (k > 0)
                {
                    float jitter = careful ? (float)(rng.NextDouble() - 0.5) * 0.08f : (float)(rng.NextDouble() - 0.5) * 0.7f;
                    s.Tick(Math.Max(0.1f, p.roll.beatSeconds + jitter));
                }

                s.Knead();
            }

            s.Stop();
            return BatchRules.Roll(b, s.Outcome, p);
        }

        private static LeafBatch Wok(LeafBatch b, bool careful, Random rng, ProcessingParams p)
        {
            var s = new WokSession(p.wok, b.id);
            float delay = careful ? 0f : 0.35f + (float)rng.NextDouble() * 0.4f;
            var history = new Queue<float>();
            const float dt = 0.02f;
            while (!s.Done)
            {
                // A sloppy cook reacts to what the heat was a moment ago.
                history.Enqueue(s.Heat - s.WindowCenter);
                float seen = history.Count * dt > delay ? history.Dequeue() : 0f;
                bool stoke = careful ? s.Heat < s.WindowCenter - 0.02f : seen < (float)(rng.NextDouble() - 0.5) * 0.1f;
                s.Tick(dt, stoke);
            }

            return BatchRules.Fix(b, s.Outcome, p);
        }

        private static LeafBatch Oxidize(LeafBatch b, float target, bool careful, Random rng, ProcessingParams p, ref float interactions)
        {
            b = Act(b, BatchAction.Oxidize, p, ref interactions);
            float stopAt = careful ? target : target + (float)(rng.NextDouble() * 0.25 - 0.05);
            double watched = 0;
            int guard = 0;
            while (b.oxidation < stopAt && b.oxidation < 1f && guard++ < 1000)
            {
                b = BatchRules.Elapse(b, 1, p);
                watched += 1;
            }

            // Watching the basket is active time (game minutes → real seconds).
            LeafBatch n = b.Clone();
            n.activeSeconds += (float)(watched / Math.Max(0.01f, p.gameMinutesPerRealSecond));
            return Act(n, BatchAction.TakeOut, p, ref interactions);
        }

        private static readonly BatchAction[] RandomActions =
            { BatchAction.Wither, BatchAction.Roll, BatchAction.Oxidize, BatchAction.Fix, BatchAction.Dry, BatchAction.TakeOut };

        private static LeafBatch RandomPath(LeafBatch b, ProcessingParams p, Random rng, ref float interactions)
        {
            for (int i = 0; i < 30; i++)
            {
                if (b.IsDry(p.dry) && b.place == BatchPlace.Shelf) break;
                var allowed = new List<BatchAction>();
                foreach (BatchAction a in RandomActions)
                {
                    if (BatchRules.Check(b, a, p).ok) allowed.Add(a);
                }

                if (allowed.Count > 0)
                {
                    BatchAction pick = allowed[rng.Next(allowed.Count)];
                    if (pick == BatchAction.Roll) b = Roll(b, (float)rng.NextDouble(), false, rng, p);
                    else if (pick == BatchAction.Fix) b = Wok(b, false, rng, p);
                    else b = Act(b, pick, p, ref interactions);
                }

                b = BatchRules.Elapse(b, rng.Next(0, 300), p);
            }

            // Finally, like any player: put it in the dryer until it is done.
            if (b.place != BatchPlace.Shelf) b = Act(b, BatchAction.TakeOut, p, ref interactions);
            if (!b.IsWithered(p.wither))
            {
                b = Act(b, BatchAction.Wither, p, ref interactions);
                b = BatchRules.Elapse(b, 600, p);
                b = Act(b, BatchAction.TakeOut, p, ref interactions);
            }

            if (!b.IsDry(p.dry))
            {
                b = Act(b, BatchAction.Dry, p, ref interactions);
                b = BatchRules.Elapse(b, p.dry.minutesToDry + 10, p);
                b = Act(b, BatchAction.TakeOut, p, ref interactions);
            }

            return b;
        }
    }
}
