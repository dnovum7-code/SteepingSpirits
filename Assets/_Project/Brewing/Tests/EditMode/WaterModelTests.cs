using System.Collections.Generic;
using NUnit.Framework;
using SteepingSpirits.Brewing.Core;

namespace SteepingSpirits.Brewing.Tests
{
    // Uses only NUnit features available in Unity's bundled NUnit (3.5).
    public class WaterModelTests
    {
        private const float Dt = 1f / 60f;

        private static WaterModel NewWater(out WaterParams parameters)
        {
            parameters = new WaterParams();
            return new WaterModel(parameters, new BoilStageThresholds());
        }

        private static float RunUntil(WaterModel water, System.Func<bool> condition, float maxSeconds)
        {
            float t = 0f;
            while (!condition() && t < maxSeconds)
            {
                water.Tick(Dt);
                t += Dt;
            }

            return t;
        }

        [Test]
        public void StartsAtRoomTemperatureStillAndFresh()
        {
            WaterModel water = NewWater(out WaterParams p);

            Assert.AreEqual(p.roomTemperature, water.Temperature, 0.0001f);
            Assert.AreEqual(BoilStage.Still, water.Stage);
            Assert.IsFalse(water.IsStale);
            Assert.IsFalse(water.HeatOn);
        }

        [Test]
        public void DefaultHeatingReachesRagingWavesWithin10To16Seconds()
        {
            WaterModel water = NewWater(out _);
            water.SetHeat(true);

            float t = RunUntil(water, () => water.Stage == BoilStage.RagingWaves, 30f);

            Assert.That(t, Is.InRange(10f, 16f));
        }

        [TestCase(20f, BoilStage.Still)]
        [TestCase(69.9f, BoilStage.Still)]
        [TestCase(70f, BoilStage.ShrimpEyes)]
        [TestCase(74.9f, BoilStage.ShrimpEyes)]
        [TestCase(75f, BoilStage.CrabEyes)]
        [TestCase(80f, BoilStage.FishEyes)]
        [TestCase(85f, BoilStage.StringOfPearls)]
        [TestCase(94.9f, BoilStage.StringOfPearls)]
        [TestCase(95f, BoilStage.RagingWaves)]
        [TestCase(100f, BoilStage.RagingWaves)]
        public void ThresholdsMapToStages(float temperature, BoilStage expected)
        {
            Assert.AreEqual(expected, new BoilStageThresholds().Classify(temperature));
        }

        [Test]
        public void StageChangedFiresForEveryStageInOrderWhileHeating()
        {
            WaterModel water = NewWater(out _);
            var seen = new List<BoilStage>();
            water.StageChanged += (previous, current) => seen.Add(current);
            water.SetHeat(true);

            RunUntil(water, () => water.Stage == BoilStage.RagingWaves, 30f);

            CollectionAssert.AreEqual(new[]
            {
                BoilStage.ShrimpEyes, BoilStage.CrabEyes, BoilStage.FishEyes,
                BoilStage.StringOfPearls, BoilStage.RagingWaves
            }, seen);
        }

        [Test]
        public void BoilingLongerThanLimitMakesWaterStale()
        {
            WaterModel water = NewWater(out WaterParams p);
            int staleEvents = 0;
            water.BecameStale += () => staleEvents++;
            water.SetHeat(true);

            RunUntil(water, () => water.IsBoiling, 30f);
            RunUntil(water, () => water.BoilingSeconds >= p.staleAfterSeconds - 0.5f, 30f);
            Assert.IsFalse(water.IsStale, "not stale before the limit");

            RunUntil(water, () => water.IsStale, 5f);
            Assert.IsTrue(water.IsStale);
            Assert.AreEqual(1, staleEvents);
            Assert.AreEqual(p.staleAromaFactor, water.AromaCeiling, 0.0001f);
        }

        [Test]
        public void LadleBackLowersTemperatureAndResetsBoilCounter()
        {
            WaterModel water = NewWater(out WaterParams p);
            water.SetHeat(true);
            RunUntil(water, () => water.BoilingSeconds > 3f, 30f);

            float before = water.Temperature;
            water.LadleBack();

            Assert.AreEqual(before - p.ladleCooling, water.Temperature, 0.001f);
            Assert.AreEqual(0f, water.BoilingSeconds, 0.0001f);
        }

        [Test]
        public void LadlingKeepsWaterFreshButStaleWaterStaysStale()
        {
            WaterModel water = NewWater(out WaterParams p);
            water.SetHeat(true);
            RunUntil(water, () => water.IsBoiling, 30f);

            // Ladling every few seconds keeps the boil counter below the limit.
            for (int i = 0; i < 5; i++)
            {
                RunUntil(water, () => water.BoilingSeconds > p.staleAfterSeconds - 2f, 30f);
                water.LadleBack();
            }
            Assert.IsFalse(water.IsStale);

            RunUntil(water, () => water.IsStale, 30f);
            water.LadleBack();
            Assert.IsTrue(water.IsStale);
        }

        [Test]
        public void RefillMakesWaterFreshAndCold()
        {
            WaterModel water = NewWater(out WaterParams p);
            water.SetHeat(true);
            RunUntil(water, () => water.IsStale, 40f);

            water.Refill();

            Assert.IsFalse(water.IsStale);
            Assert.IsFalse(water.HeatOn);
            Assert.AreEqual(p.roomTemperature, water.Temperature, 0.0001f);
            Assert.AreEqual(BoilStage.Still, water.Stage);
        }

        [Test]
        public void WithoutFireWaterCoolsExponentiallyTowardsRoom()
        {
            WaterModel water = NewWater(out WaterParams p);
            water.SetHeat(true);
            RunUntil(water, () => water.IsBoiling, 30f);
            water.SetHeat(false);

            float t = RunUntil(water, () => false, 10f);
            double expected = p.roomTemperature
                              + (p.boilingPoint - p.roomTemperature) * System.Math.Exp(-p.coolingRate * t);

            Assert.AreEqual(expected, water.Temperature, 0.05);
            Assert.Greater(water.Temperature, p.roomTemperature);
            Assert.AreEqual(0f, water.BoilingSeconds, 0.0001f);
        }

        [Test]
        public void FrameRateDoesNotChangeHeatingResult()
        {
            var coarse = new WaterModel(new WaterParams(), new BoilStageThresholds());
            var fine = new WaterModel(new WaterParams(), new BoilStageThresholds());
            coarse.SetHeat(true);
            fine.SetHeat(true);

            for (int i = 0; i < 30; i++) coarse.Tick(0.1f);
            for (int i = 0; i < 300; i++) fine.Tick(0.01f);

            Assert.AreEqual(coarse.Temperature, fine.Temperature, 0.01f);
        }
    }
}
