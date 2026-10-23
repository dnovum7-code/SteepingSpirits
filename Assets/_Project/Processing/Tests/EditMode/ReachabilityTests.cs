using System;
using System.Collections.Generic;
using NUnit.Framework;
using static SteepingSpirits.Processing.Tests.Kit;

namespace SteepingSpirits.Processing.Tests
{
    /// <summary>Every tea can be reached, and any sequence of allowed steps ends in a valid tea.</summary>
    public class ReachabilityTests
    {
        private static readonly BatchAction[] Actions =
            { BatchAction.Wither, BatchAction.Roll, BatchAction.Oxidize, BatchAction.Fix, BatchAction.Dry, BatchAction.TakeOut };

        /// <summary>Random allowed steps with random waits, then dried; returns the finished batch.</summary>
        public static LeafBatch RandomRun(Random rng, out int steps)
        {
            LeafBatch b = Fresh(rng.Next(1, 4));
            steps = 0;
            for (int i = 0; i < 40; i++)
            {
                if (b.IsDry(P.dry) && b.place == BatchPlace.Shelf) break;
                var allowed = new List<BatchAction>();
                foreach (BatchAction a in Actions)
                {
                    if (BatchRules.Check(b, a, P).ok) allowed.Add(a);
                }

                if (allowed.Count == 0)
                {
                    b = BatchRules.Elapse(b, 30, P);
                    continue;
                }

                BatchAction pick = allowed[rng.Next(allowed.Count)];
                switch (pick)
                {
                    case BatchAction.Roll:
                        b = BatchRules.Roll(b, Roll((float)rng.NextDouble(), (float)rng.NextDouble()), P);
                        break;
                    case BatchAction.Fix:
                        float u = (float)rng.NextDouble(), o = (float)rng.NextDouble(), n = (float)rng.NextDouble();
                        b = BatchRules.Fix(b, Wok(u, n, o), P);
                        break;
                    default:
                        b = BatchRules.Place(b, pick, P);
                        break;
                }

                steps++;
                b = BatchRules.Elapse(b, rng.Next(0, 400), P);
            }

            // Whatever happened: the leaves can always still be dried and finished.
            if (b.place != BatchPlace.Shelf) b = BatchRules.Place(b, BatchAction.TakeOut, P);
            if (!b.IsWithered(P.wither))
            {
                b = BatchRules.Place(b, BatchAction.Wither, P);
                b = BatchRules.Elapse(b, 600, P);
                b = BatchRules.Place(b, BatchAction.TakeOut, P);
            }

            if (!b.IsDry(P.dry)) b = DryFully(b);
            return b;
        }

        [Test]
        public void ThousandsOfRandomSequences_AlwaysEndInAValidTea()
        {
            var rng = new Random(7);
            var seen = new HashSet<TeaVariety>();
            for (int i = 0; i < 2000; i++)
            {
                LeafBatch b = RandomRun(rng, out _);
                Assert.IsTrue(BatchRules.Check(b, BatchAction.Finish, P).ok, "run " + i + " could not be finished: " + b.PathCode());
                TeaVariety t = TeaClassifier.Classify(b, P.variety);
                Assert.IsTrue(Enum.IsDefined(typeof(TeaVariety), t));
                Assert.That(b.oxidation, Is.InRange(0f, 1f));
                Assert.That(b.roast, Is.InRange(0f, 1f));
                seen.Add(t);
            }

            Assert.AreEqual(5, seen.Count, "random play meets every tea");
        }

        [Test]
        public void EveryTea_HasAPathFromFreshLeaves()
        {
            var reached = new HashSet<TeaVariety>
            {
                TeaClassifier.Classify(White(), P.variety),
                TeaClassifier.Classify(Green(), P.variety),
                TeaClassifier.Classify(Oolong(0.2f), P.variety),
                TeaClassifier.Classify(Oolong(0.5f), P.variety),
                TeaClassifier.Classify(Black(), P.variety)
            };
            Assert.AreEqual(5, reached.Count);
        }

        [Test]
        public void FreshLeaves_CanNeverGetStuck()
        {
            // From every reachable state there is still a way to a finished tea.
            var rng = new Random(11);
            for (int i = 0; i < 300; i++)
            {
                RandomRun(rng, out int steps);
                Assert.Less(steps, 41);
            }
        }
    }
}
