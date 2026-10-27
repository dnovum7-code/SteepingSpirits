using System;
using System.Collections.Generic;
using NUnit.Framework;
using SteepingSpirits.Brewing.Core;

namespace SteepingSpirits.Brewing.Tests
{
    public class BrewSessionTests
    {
        private const float Dt = 1f / 60f;

        private static BrewSession NewSession() => new BrewSession(new BrewConfig(), BrewTestUtil.AllTeas());

        private static void Run(BrewSession s, float seconds)
        {
            for (float t = 0f; t < seconds; t += Dt) s.Tick(Dt);
        }

        private static void RunUntil(BrewSession s, Func<bool> condition, float maxSeconds = 60f)
        {
            for (float t = 0f; !condition() && t < maxSeconds; t += Dt) s.Tick(Dt);
        }

        /// <summary>Heat to a target, switch the fire off, optionally pre-warm, pour, steep, lift.</summary>
        private static BrewResult Brew(BrewSession s, int tea, float kettleTarget, bool prewarm, float steepSeconds)
        {
            Assert.IsTrue(s.SelectTea(tea));
            s.ToggleHeat();
            RunUntil(s, () => s.Water.Temperature >= kettleTarget);
            s.ToggleHeat();
            if (prewarm)
            {
                Assert.IsTrue(s.StartPrewarm());
                RunUntil(s, () => s.Phase == BrewPhase.HeatWater);
                Assert.IsTrue(s.VesselPrewarmed);
            }

            Assert.IsTrue(s.Pour());
            RunUntil(s, () => s.Phase == BrewPhase.Steep);
            Run(s, steepSeconds);
            Assert.IsTrue(s.LiftLeaves());
            return s.LastResult;
        }

        [Test]
        public void FullFlowVisitsPhasesInOrder()
        {
            BrewSession s = NewSession();
            var phases = new List<BrewPhase>();
            s.PhaseChanged += (previous, next) => phases.Add(next);

            Brew(s, 0, 100f, true, 8f);
            s.NextCup();

            CollectionAssert.AreEqual(new[]
            {
                BrewPhase.HeatWater, BrewPhase.PreWarm, BrewPhase.HeatWater, BrewPhase.Pour,
                BrewPhase.Steep, BrewPhase.Result, BrewPhase.SelectTea
            }, phases);
        }

        [Test]
        public void TypicalBrewsReachGoodTiersForEachTea()
        {
            // Black: boiling, pre-warmed (97 °C start). White: fish eyes (≈83 → 80 °C).
            // Green: crab/fish eyes boundary (≈80 → 77 °C), pre-warmed.
            BrewResult black = Brew(NewSession(), 0, 100f, true, 8f);
            BrewResult white = Brew(NewSession(), 1, 88f, false, 13f);
            BrewResult green = Brew(NewSession(), 2, 79f, true, 6f);

            Assert.GreaterOrEqual(black.Tier, QualityTier.Harmonious, "black");
            Assert.GreaterOrEqual(white.Tier, QualityTier.Harmonious, "white");
            Assert.GreaterOrEqual(green.Tier, QualityTier.Harmonious, "green");
        }

        [Test]
        public void PrewarmingReducesPourLoss()
        {
            BrewResult cold = Brew(NewSession(), 0, 100f, false, 1f);
            BrewResult warm = Brew(NewSession(), 0, 100f, true, 1f);
            var pour = new PourParams();

            Assert.AreEqual(cold.PourTemperature - pour.lossCold, cold.SteepStartTemperature, 0.01f);
            Assert.AreEqual(warm.PourTemperature - pour.lossPrewarmed, warm.SteepStartTemperature, 0.01f);
            Assert.IsTrue(warm.Prewarmed);
        }

        [Test]
        public void StaleWaterIsRecordedAndLowersAroma()
        {
            BrewSession s = NewSession();
            s.SelectTea(0);
            s.ToggleHeat();
            RunUntil(s, () => s.Water.IsStale);
            s.Pour();
            RunUntil(s, () => s.Phase == BrewPhase.Steep);
            Run(s, 30f);
            s.LiftLeaves();

            Assert.IsTrue(s.LastResult.StaleWater);
            Assert.LessOrEqual(s.LastResult.Aroma, new WaterParams().staleAromaFactor + 1e-4f);
        }

        [Test]
        public void CommandsOutsideTheirPhaseAreIgnored()
        {
            BrewSession s = NewSession();

            Assert.IsFalse(s.Pour(), "no pour before a tea is chosen");
            Assert.IsFalse(s.LiftLeaves());
            Assert.IsFalse(s.ToggleHeat());
            Assert.IsFalse(s.SelectTea(7));
            Assert.AreEqual(BrewPhase.SelectTea, s.Phase);

            s.SelectTea(1);
            Assert.IsFalse(s.SelectTea(0), "tea is fixed once heating started");
            Assert.IsTrue(s.StartPrewarm());
            Assert.IsFalse(s.Pour(), "no pour while pre-warming");
            Assert.IsFalse(s.StartPrewarm());
        }

        [Test]
        public void ResultCarriesSessionData()
        {
            BrewSession s = NewSession();
            s.SelectTea(2);
            s.MarkThermometerUsed();
            s.ToggleHeat();
            RunUntil(s, () => s.Water.Temperature >= 80f);
            s.Pour();
            RunUntil(s, () => s.Phase == BrewPhase.Steep);
            Run(s, 0.5f);
            s.LiftLeaves();
            BrewResult r = s.LastResult;

            Assert.AreEqual("green", r.TeaId);
            Assert.AreEqual(0, r.InfusionIndex);
            Assert.IsTrue(r.ThermometerUsed);
            Assert.AreEqual(QualityTier.Flat, r.Tier, "lifted after half a second");
            Assert.AreEqual(BrewHint.TooShort, r.Hint);
            Assert.Greater(r.TotalSeconds, r.SteepSeconds);
        }

        [Test]
        public void HintsPointAtTheMainProblem()
        {
            Assert.AreEqual(BrewHint.TooHot, Brew(NewSession(), 2, 100f, true, 6f).Hint, "green with boiling water");
            Assert.AreEqual(BrewHint.TooLong, Brew(NewSession(), 0, 100f, true, 30f).Hint, "black steeped far too long");
            Assert.AreEqual(BrewHint.TooCold, Brew(NewSession(), 0, 60f, false, 8f).Hint, "black with lukewarm water");
        }

        [Test]
        public void RestartGivesFreshColdWater()
        {
            BrewSession s = NewSession();
            s.SelectTea(0);
            s.ToggleHeat();
            Run(s, 20f);

            s.Restart();

            Assert.AreEqual(BrewPhase.SelectTea, s.Phase);
            Assert.AreEqual(new WaterParams().roomTemperature, s.Water.Temperature, 1e-4f);
            Assert.IsFalse(s.Water.HeatOn);
        }

        [Test]
        public void NoInputHistoryLeadsOutsideTheFourTiers()
        {
            var allowed = new Dictionary<BrewPhase, BrewPhase[]>
            {
                { BrewPhase.SelectTea, new[] { BrewPhase.HeatWater } },
                { BrewPhase.HeatWater, new[] { BrewPhase.PreWarm, BrewPhase.Pour, BrewPhase.SelectTea } },
                { BrewPhase.PreWarm, new[] { BrewPhase.HeatWater, BrewPhase.SelectTea } },
                { BrewPhase.Pour, new[] { BrewPhase.Steep, BrewPhase.SelectTea } },
                { BrewPhase.Steep, new[] { BrewPhase.Result, BrewPhase.SelectTea } },
                { BrewPhase.Result, new[] { BrewPhase.SelectTea, BrewPhase.HeatWater } }
            };

            var rng = new Random(1234);
            int results = 0;
            for (int run = 0; run < 150; run++)
            {
                BrewSession s = NewSession();
                s.PhaseChanged += (previous, next) =>
                    Assert.Contains(next, allowed[previous], $"illegal transition {previous} → {next}");
                s.Finished += r =>
                {
                    results++;
                    Assert.That(Enum.IsDefined(typeof(QualityTier), r.Tier));
                    Assert.That(r.Harmony, Is.InRange(0f, 1f));
                    Assert.IsFalse(float.IsNaN(r.Aroma) || float.IsNaN(r.Bitterness));
                };

                for (int step = 0; step < 4000; step++)
                {
                    switch (rng.Next(14))
                    {
                        case 0: s.SelectTea(rng.Next(-1, 4)); break;
                        case 1: s.ToggleHeat(); break;
                        case 2: s.LadleBack(); break;
                        case 3: s.StartPrewarm(); break;
                        case 4: s.Pour(); break;
                        case 5: s.LiftLeaves(); break;
                        case 6: s.NextCup(); break;
                        case 7: if (rng.Next(20) == 0) s.Restart(); break;
                        case 8: s.MarkThermometerUsed(); break;
                    }

                    s.Tick((float)(rng.NextDouble() * 0.2));
                }
            }

            Assert.Greater(results, 50, "fuzzing should finish plenty of brews");
        }
    }
}
