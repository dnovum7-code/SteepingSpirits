using System;
using System.Collections.Generic;
using NUnit.Framework;
using SteepingSpirits.Brewing.Core;

namespace SteepingSpirits.Brewing.Tests
{
    public class BrewSessionPhase2Tests
    {
        private const float Dt = 1f / 60f;
        private const int Black = 0, White = 1, Green = 2, Oolong = 3;

        private static BrewSession NewSession() => new BrewSession(new BrewConfig(), BrewTestUtil.AllTeasWithOolong());

        private static void Run(BrewSession s, float seconds)
        {
            for (float t = 0f; t < seconds; t += Dt) s.Tick(Dt);
        }

        private static void RunUntil(BrewSession s, Func<bool> condition, float maxSeconds = 90f)
        {
            for (float t = 0f; !condition() && t < maxSeconds; t += Dt) s.Tick(Dt);
        }

        /// <summary>Plays the current infusion ideally: pour at the right kettle temperature, lift at the calibrated optimum.</summary>
        private static BrewResult PlayInfusion(BrewSession s, bool catchSpark = false, float followExtra = 0f)
        {
            float target = s.InfusionTea.IdealMid + (s.VesselPrewarmed ? 3f : 8f);
            if (!s.Water.HasWaterForPour) s.RefillWater();
            if (!s.Water.HeatOn) s.ToggleHeat();
            RunUntil(s, () => s.Water.Temperature >= Math.Min(target, 100f));
            s.ToggleHeat();
            Assert.IsTrue(s.Pour(), "pour");
            RunUntil(s, () => s.Phase == BrewPhase.Steep);

            float lift = s.Calibration.OptimalSeconds;
            for (float t = 0f; t < lift + followExtra; t += Dt)
            {
                s.Tick(Dt);
                if (catchSpark && s.Spark.State == SparkState.Visible) s.CatchSpark();
            }

            Assert.IsTrue(s.LiftLeaves());
            return s.LastResult;
        }

        [Test]
        public void InfusionIndexCountsAndResidualShrinksByDissolvedAroma()
        {
            BrewSession s = NewSession();
            s.SelectTea(Black);
            float before = s.Leaves.ResidualExtract;

            BrewResult first = PlayInfusion(s);
            Assert.AreEqual(0, first.InfusionIndex);
            Assert.AreEqual(before - first.Aroma, s.Leaves.ResidualExtract, 1e-4f);
            Assert.IsTrue(first.CanInfuseAgain);

            Assert.IsTrue(s.NextInfusion());
            Assert.AreEqual(1, s.InfusionIndex);
            Assert.IsTrue(s.VesselPrewarmed, "vessel still warm from the last infusion");
            BrewResult second = PlayInfusion(s);
            Assert.AreEqual(1, second.InfusionIndex);
            Assert.Less(second.Aroma, first.Aroma, "later infusions are lighter");
        }

        [Test]
        public void EachInfusionKeepsItsOwnProfile()
        {
            BrewSession s = NewSession();
            s.SelectTea(White);
            for (int i = 0; i < 3; i++)
            {
                PlayInfusion(s);
                if (i < 2) Assert.IsTrue(s.NextInfusion());
            }

            Assert.AreEqual(3, s.History.Count);
            for (int i = 0; i < 3; i++) Assert.AreEqual(i, s.History[i].InfusionIndex);
            Assert.AreEqual(InfusionCharacter.Bright, s.History[0].Character);
            Assert.AreNotEqual(s.History[0].Aroma, s.History[2].Aroma);
        }

        [Test]
        public void InfusionsStopAtMaxButNeverFail()
        {
            BrewSession s = NewSession();
            s.SelectTea(Green);
            int brewed = 0;
            do
            {
                BrewResult r = PlayInfusion(s);
                brewed++;
                Assert.That(Enum.IsDefined(typeof(QualityTier), r.Tier));
            } while (s.NextInfusion());

            Assert.AreEqual(TeaPresets.Green().maxInfusions, brewed);
            Assert.IsFalse(s.LastResult.CanInfuseAgain);
            Assert.IsTrue(s.NextCup(), "a new tea is always possible");
        }

        [Test]
        public void OolongGetsBetterWithIdealPlay()
        {
            BrewSession s = NewSession();
            s.SelectTea(Oolong);
            var harmonies = new List<float>();
            for (int i = 0; i < 3; i++)
            {
                harmonies.Add(PlayInfusion(s).Harmony);
                s.NextInfusion();
            }

            Assert.Less(harmonies[0], harmonies[1]);
            Assert.Less(harmonies[1], harmonies[2]);
            Assert.GreaterOrEqual(harmonies[2], new QualityParams().harmoniousThreshold);
        }

        [Test]
        public void CatchingTheSparkAddsABonusMissingItCostsNothing()
        {
            BrewSession caught = NewSession();
            caught.SelectTea(Black);
            BrewResult withSpark = PlayInfusion(caught, catchSpark: true);

            BrewSession missed = NewSession();
            missed.SelectTea(Black);
            BrewResult without = PlayInfusion(missed);

            Assert.IsTrue(withSpark.SparkAppeared);
            Assert.IsTrue(withSpark.MemoryCaught);
            Assert.Greater(withSpark.HarmonyBonus, 0f);
            Assert.AreEqual(withSpark.BaseHarmony, without.BaseHarmony, 1e-4f, "same steep, same base");
            Assert.GreaterOrEqual(withSpark.Harmony, without.Harmony);
            Assert.IsTrue(without.SparkAppeared);
            Assert.IsFalse(without.MemoryCaught);
            Assert.AreEqual(0f, without.HarmonyBonus, 1e-6f);
        }

        [Test]
        public void FollowingTheSparkDeepensTheMemoryButRisksBitterness()
        {
            // Green: followed far beyond the optimum – deeper memory, but more bitterness.
            BrewSession quick = NewSession();
            quick.SelectTea(Green);
            BrewResult a = PlayInfusion(quick, catchSpark: true);

            BrewSession follow = NewSession();
            follow.SelectTea(Green);
            BrewResult b = PlayInfusion(follow, catchSpark: true, followExtra: 4f);

            Assert.Greater(b.MemoryDepth, a.MemoryDepth);
            Assert.Greater(b.Bitterness, a.Bitterness);
            Assert.Less(b.BaseHarmony, a.BaseHarmony, "the risk is real");
        }

        [Test]
        public void PouringUsesWaterAndAnEmptyKettleJustNeedsARefill()
        {
            BrewSession s = NewSession();
            var water = new WaterParams();
            s.SelectTea(Black);
            int pours = 0;
            while (s.Water.HasWaterForPour)
            {
                Assert.IsTrue(s.Pour());
                pours++;
                RunUntil(s, () => s.Phase == BrewPhase.Steep);
                s.LiftLeaves();
                if (!s.NextInfusion())
                {
                    s.NextCup();
                    s.SelectTea(Black);
                }
            }

            Assert.AreEqual((int)Math.Floor(water.capacity / water.pourVolume + 1e-4f), pours);
            Assert.IsFalse(s.Pour(), "no water – no pour, nothing breaks");
            Assert.AreEqual(BrewPhase.HeatWater, s.Phase);
            Assert.IsTrue(s.RefillWater());
            Assert.IsTrue(s.Pour());
        }

        [Test]
        public void NoInputHistoryLeadsOutsideTheFourTiersWithPhaseTwo()
        {
            var rng = new Random(99);
            int results = 0, nextInfusions = 0;
            for (int run = 0; run < 120; run++)
            {
                BrewSession s = NewSession();
                s.Finished += r =>
                {
                    results++;
                    Assert.That(Enum.IsDefined(typeof(QualityTier), r.Tier));
                    Assert.That(r.Harmony, Is.InRange(0f, 1f));
                    Assert.That(r.BaseHarmony, Is.InRange(0f, 1f));
                    Assert.GreaterOrEqual(r.ResidualAfter, 0f);
                };

                for (int step = 0; step < 4000; step++)
                {
                    switch (rng.Next(16))
                    {
                        case 0: s.SelectTea(rng.Next(-1, 5)); break;
                        case 1: s.ToggleHeat(); break;
                        case 2: s.LadleBack(); break;
                        case 3: s.StartPrewarm(); break;
                        case 4: s.Pour(); break;
                        case 5: s.LiftLeaves(); break;
                        case 6: s.NextCup(); break;
                        case 7: if (s.NextInfusion()) nextInfusions++; break;
                        case 8: s.CatchSpark(); break;
                        case 9: s.RefillWater(); break;
                        case 10: if (rng.Next(25) == 0) s.Restart(); break;
                    }

                    s.Tick((float)(rng.NextDouble() * 0.2));
                }
            }

            Assert.Greater(results, 50);
            Assert.Greater(nextInfusions, 5, "fuzzing should reach later infusions");
        }
    }
}
