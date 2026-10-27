using System;
using NUnit.Framework;
using SteepingSpirits.Brewing.Core;
using static SteepingSpirits.Brewing.Tests.BrewTestUtil;

namespace SteepingSpirits.Brewing.Tests
{
    public class ProcessedBrewingTests
    {
        private static readonly ProcessedBrewParams K = new ProcessedBrewParams();

        private static TeaParams PresetOf(BrewBase b)
        {
            switch (b)
            {
                case BrewBase.White: return TeaPresets.White();
                case BrewBase.Green: return TeaPresets.Green();
                case BrewBase.Oolong: return TeaPresets.Oolong();
                default: return TeaPresets.Black();
            }
        }

        private static float NeutralOxidation(BrewBase b) => b == BrewBase.Oolong ? K.oolongPivotOxidation : b == BrewBase.Black ? 0.92f : 0.02f;

        private static ProcessedTeaInfo Info(BrewBase b, float ox, int tier = 2, BrewNotes notes = BrewNotes.None) =>
            new ProcessedTeaInfo { baseTea = b, oxidation = ox, tier = tier, notes = notes };

        private static void AssertSame(TeaParams a, TeaParams b)
        {
            Assert.AreEqual(a.idealMin, b.idealMin, 1e-5);
            Assert.AreEqual(a.idealMax, b.idealMax, 1e-5);
            Assert.AreEqual(a.aromaRate, b.aromaRate, 1e-6);
            Assert.AreEqual(a.bitterOnsetSeconds, b.bitterOnsetSeconds, 1e-6);
            Assert.AreEqual(a.bitterRate, b.bitterRate, 1e-6);
            Assert.AreEqual(a.heatSensitivity, b.heatSensitivity, 1e-6);
            Assert.AreEqual(a.bitterTolerance, b.bitterTolerance, 1e-6);
            Assert.AreEqual(a.leafCapacity, b.leafCapacity, 1e-6);
            Assert.AreEqual(a.leafSaturation, b.leafSaturation, 1e-6);
            Assert.AreEqual(a.maxInfusions, b.maxInfusions);
            Assert.AreEqual(a.infusionSteps.Length, b.infusionSteps.Length);
            for (int i = 0; i < a.infusionSteps.Length; i++) Assert.AreEqual(a.infusionSteps[i].aromaMaxFactor, b.infusionSteps[i].aromaMaxFactor, 1e-6);
        }

        [Test]
        public void NoNotes_TierTwo_NeutralOxidation_GivesThePresetUnchanged()
        {
            foreach (BrewBase b in Enum.GetValues(typeof(BrewBase)))
            {
                AssertSame(PresetOf(b), ProcessedBrewing.ForProcessedTea(PresetOf(b), Info(b, NeutralOxidation(b)), K));
            }
        }

        [Test]
        public void ThePresetItself_IsNeverChanged()
        {
            TeaParams preset = TeaPresets.Oolong();
            float firstAroma = preset.infusionSteps[0].aromaMaxFactor;
            ProcessedBrewing.ForProcessedTea(preset, Info(BrewBase.Oolong, 0.7f, 3, BrewNotes.Floral | BrewNotes.Roasty | BrewNotes.Delicate), K);
            AssertSame(TeaPresets.Oolong(), preset);
            Assert.AreEqual(firstAroma, preset.infusionSteps[0].aromaMaxFactor);
        }

        [Test]
        public void DarkerOolong_TakesHotterWater_Monotonic()
        {
            float last = float.MinValue;
            for (int i = 0; i <= 20; i++)
            {
                float ox = 0.1f + i * 0.03f;
                TeaParams t = ProcessedBrewing.ForProcessedTea(TeaPresets.Oolong(), Info(BrewBase.Oolong, ox), K);
                Assert.GreaterOrEqual(t.idealMin, last, "ox " + ox);
                last = t.idealMin;
            }

            TeaParams light = ProcessedBrewing.ForProcessedTea(TeaPresets.Oolong(), Info(BrewBase.Oolong, 0.2f), K);
            TeaParams dark = ProcessedBrewing.ForProcessedTea(TeaPresets.Oolong(), Info(BrewBase.Oolong, 0.5f), K);
            Assert.Greater(dark.IdealMid - light.IdealMid, 4f, "light and dark oolong brew noticeably differently");
            Assert.Greater(light.heatSensitivity, dark.heatSensitivity, "light oolong is more sensitive to hot water");
        }

        [Test]
        public void NoCombinationOfNotes_MovesTheWindowMoreThanTheCap()
        {
            for (int mask = 0; mask < 64; mask++)
            foreach (BrewBase b in new[] { BrewBase.White, BrewBase.Green, BrewBase.Black })
            {
                TeaParams preset = PresetOf(b);
                TeaParams t = ProcessedBrewing.ForProcessedTea(preset, Info(b, NeutralOxidation(b), 2, (BrewNotes)mask), K);
                Assert.LessOrEqual(Math.Abs(t.idealMin - preset.idealMin), K.noteMaxShift + 1e-4, b + " " + (BrewNotes)mask);
                Assert.LessOrEqual(t.idealMax, K.boilingPoint);
                Assert.GreaterOrEqual(t.idealMax - t.idealMin, K.minWindowWidth - 1e-4);
            }
        }

        [Test]
        public void HigherTier_MeansMoreSubstanceAndForgiveness()
        {
            TeaParams gut = ProcessedBrewing.ForProcessedTea(TeaPresets.Black(), Info(BrewBase.Black, 0.92f, 1), K);
            TeaParams erlesen = ProcessedBrewing.ForProcessedTea(TeaPresets.Black(), Info(BrewBase.Black, 0.92f, 3), K);
            Assert.Greater(erlesen.leafCapacity, gut.leafCapacity);
            Assert.Greater(erlesen.bitterTolerance, gut.bitterTolerance);
            Assert.Greater(erlesen.aromaRate, gut.aromaRate);
        }

        [Test]
        public void Notes_HaveTheirEffect()
        {
            TeaParams p = TeaPresets.Green();
            Assert.Less(ProcessedBrewing.ForProcessedTea(p, Info(BrewBase.Green, 0.02f, 2, BrewNotes.Fresh), K).IdealMid, p.IdealMid);
            Assert.Greater(ProcessedBrewing.ForProcessedTea(p, Info(BrewBase.Green, 0.02f, 2, BrewNotes.Roasty), K).IdealMid, p.IdealMid);
            Assert.Less(ProcessedBrewing.ForProcessedTea(p, Info(BrewBase.Green, 0.02f, 2, BrewNotes.Honeyed), K).bitterRate, p.bitterRate);
            Assert.Greater(ProcessedBrewing.ForProcessedTea(p, Info(BrewBase.Green, 0.02f, 2, BrewNotes.Malty), K).bitterTolerance, p.bitterTolerance);
            Assert.Greater(ProcessedBrewing.ForProcessedTea(p, Info(BrewBase.Green, 0.02f, 2, BrewNotes.Delicate), K).leafSaturation, p.leafSaturation);
            TeaParams floral = ProcessedBrewing.ForProcessedTea(p, Info(BrewBase.Green, 0.02f, 2, BrewNotes.Floral), K);
            Assert.AreEqual(K.floralFirstAromaMax, floral.StepFor(0).aromaMaxFactor, 1e-6);
            Assert.AreEqual(1f, floral.StepFor(1).aromaMaxFactor, 1e-6, "only the first infusion");
            Assert.AreEqual(1f, floral.StepFor(4).aromaMaxFactor, 1e-6);
        }

        [Test]
        public void UnderOxidizedBlack_TurnsBitterSooner()
        {
            TeaParams full = ProcessedBrewing.ForProcessedTea(TeaPresets.Black(), Info(BrewBase.Black, 0.95f), K);
            TeaParams under = ProcessedBrewing.ForProcessedTea(TeaPresets.Black(), Info(BrewBase.Black, 0.8f), K);
            Assert.Less(under.bitterOnsetSeconds, full.bitterOnsetSeconds);
        }

        [Test]
        public void Vollendet_StaysReachable_ForEveryWorkshopTea()
        {
            float hottestStart = 100f - new PourParams().lossPrewarmed;
            int checkedTeas = 0;
            foreach (BrewBase b in Enum.GetValues(typeof(BrewBase)))
            foreach (float ox in b == BrewBase.Oolong ? new[] { 0.1f, 0.2f, 0.35f, 0.5f, 0.7f } : new[] { NeutralOxidation(b), 0.8f })
            for (int tier = 1; tier <= 3; tier++)
            for (int mask = 0; mask < 64; mask += 3)
            {
                TeaParams t = ProcessedBrewing.ForProcessedTea(PresetOf(b), Info(b, ox, tier, (BrewNotes)mask), K);
                CalibrationSeries s = Series(t);
                for (int i = 0; i < s.Infusions.Count; i++)
                {
                    Assert.LessOrEqual(t.ForInfusion(i).IdealMid, hottestStart + 1e-3, $"{b} ox {ox} notes {(BrewNotes)mask} infusion {i} unreachable");
                }

                // Brewed like the calibration (window centre, best moment) the best infusion is "Vollendet".
                int best = s.BestInfusion;
                InfusionCalibration c = s.For(best);
                QualityEvaluation q = BrewCalibration.Simulate(t.ForInfusion(best), Shape, Quality, Room, c.StartTemperature, c.OptimalSeconds, c.AromaMax, s.QReference);
                Assert.AreEqual(QualityTier.Perfect, q.Tier, $"{b} ox {ox} tier {tier} notes {(BrewNotes)mask}");
                Assert.Greater(s.QReference, 0.3f, "a real cup, not a degenerate one");
                checkedTeas++;
            }

            Assert.Greater(checkedTeas, 500);
        }

        [Test]
        public void DarkOolong_LaterInfusions_StayReachable_ButStillHotterThanLight()
        {
            TeaParams light = ProcessedBrewing.ForProcessedTea(TeaPresets.Oolong(), Info(BrewBase.Oolong, 0.2f), K);
            TeaParams dark = ProcessedBrewing.ForProcessedTea(TeaPresets.Oolong(), Info(BrewBase.Oolong, 0.7f, 2, BrewNotes.Roasty), K);
            for (int i = 0; i < 6; i++)
            {
                Assert.GreaterOrEqual(dark.ForInfusion(i).IdealMid, light.ForInfusion(i).IdealMid, "infusion " + i);
                Assert.LessOrEqual(dark.ForInfusion(i).IdealMid, K.reachableTop + 1e-3);
            }
        }

        /// <summary>Regression: the four base teas brew exactly as before the workshop connection.</summary>
        [TestCase("white", 0.8901324f, 14.180135f)]
        [TestCase("green", 0.8347014f, 5.999995f)]
        [TestCase("oolong", 0.88521594f, 8.840013f)]
        [TestCase("black", 0.9366701f, 7.8999934f)]
        public void BaseTeas_BrewExactlyAsBefore(string id, float qReference, float bestSeconds)
        {
            TeaParams preset = id == "white" ? TeaPresets.White() : id == "green" ? TeaPresets.Green() : id == "oolong" ? TeaPresets.Oolong() : TeaPresets.Black();
            CalibrationSeries s = Series(preset);
            Assert.AreEqual(qReference, s.QReference, 1e-5);
            Assert.AreEqual(bestSeconds, s.For(s.BestInfusion).OptimalSeconds, 1e-3);
        }
    }
}
