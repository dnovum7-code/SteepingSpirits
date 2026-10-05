using System;
using NUnit.Framework;
using SteepingSpirits.Brewing.Core;

namespace SteepingSpirits.Brewing.Tests
{
    public class InfusionSeriesTests
    {
        [Test]
        public void PrintSeriesReport()
        {
            foreach (TeaParams tea in BrewTestUtil.AllTeasWithOolong())
            {
                CalibrationSeries series = BrewTestUtil.Series(tea);
                var line = new System.Text.StringBuilder($"{tea.id}: Qref={series.QReference:0.000} best=#{series.BestInfusion + 1}");
                for (int i = 0; i < series.Infusions.Count; i++)
                {
                    InfusionCalibration c = series.Infusions[i];
                    line.Append($" | #{i + 1} H={c.BestQ / series.QReference:0.00} t={c.OptimalSeconds:0.0}s T={c.StartTemperature:0}°C Amax={c.AromaMax:0.00}");
                }

                TestContext.WriteLine(line.ToString());
            }

            Assert.Pass();
        }

        [Test]
        public void PhaseOneTeasPeakInTheFirstInfusion()
        {
            foreach (TeaParams tea in BrewTestUtil.AllTeas())
            {
                CalibrationSeries series = BrewTestUtil.Series(tea);
                Assert.AreEqual(0, series.BestInfusion, tea.id);
                Assert.AreEqual(BrewTestUtil.Calibrate(tea).QReference, series.QReference, 1e-4f,
                    tea.id + ": first infusion unchanged from Phase 1");
            }
        }

        [Test]
        public void LaterInfusionsOfPhaseOneTeasGetWeakerButNeverFail()
        {
            foreach (TeaParams tea in BrewTestUtil.AllTeas())
            {
                CalibrationSeries series = BrewTestUtil.Series(tea);
                Assert.AreEqual(tea.maxInfusions, series.Infusions.Count, tea.id);
                for (int i = 1; i < series.Infusions.Count; i++)
                {
                    Assert.LessOrEqual(series.Infusions[i].BestQ, series.Infusions[i - 1].BestQ + 1e-4f, $"{tea.id} #{i + 1}");
                }
            }
        }

        [Test]
        public void OolongImprovesOverTheFirstInfusions()
        {
            CalibrationSeries series = BrewTestUtil.Series(TeaPresets.Oolong());

            Assert.Less(series.Infusions[0].BestQ, series.Infusions[1].BestQ);
            Assert.Less(series.Infusions[1].BestQ, series.Infusions[2].BestQ);
            Assert.GreaterOrEqual(series.BestInfusion, 2, "oolong peaks in the third infusion or later");
        }

        [Test]
        public void OolongGoodWindowMovesWithEachInfusion()
        {
            TeaParams oolong = TeaPresets.Oolong();
            CalibrationSeries series = BrewTestUtil.Series(oolong);

            for (int i = 1; i < 4; i++)
            {
                Assert.Greater(series.Infusions[i].StartTemperature, series.Infusions[i - 1].StartTemperature, $"window #{i + 1} warmer");
                Assert.AreNotEqual(series.Infusions[i].OptimalSeconds, series.Infusions[i - 1].OptimalSeconds, $"timing #{i + 1} differs");
            }

            Assert.Greater(oolong.ForInfusion(2).idealMin, oolong.idealMin);
        }

        [Test]
        public void ForInfusionLeavesTheBaseTeaUntouched()
        {
            TeaParams oolong = TeaPresets.Oolong();
            float min = oolong.idealMin;
            TeaParams third = oolong.ForInfusion(2);

            Assert.AreEqual(min, oolong.idealMin);
            Assert.AreNotSame(oolong, third);
            Assert.AreEqual(TeaPresets.Black().aromaRate, TeaPresets.Black().ForInfusion(5).aromaRate, 1e-6f, "neutral steps");
        }

        [Test]
        public void AromaMaxFollowsResidualStepAndStaleWater()
        {
            TeaParams oolong = TeaPresets.Oolong();
            TeaParams black = TeaPresets.Black();
            Assert.AreEqual(0.7f, BrewCalibration.AromaMaxFor(oolong, 0, oolong.leafCapacity, false, 0.85f), 1e-5f,
                "full leaves × first-infusion step");
            Assert.AreEqual(1f, BrewCalibration.AromaMaxFor(black, 0, black.leafCapacity, false, 0.85f), 1e-5f);
            Assert.AreEqual(0.85f, BrewCalibration.AromaMaxFor(black, 0, black.leafCapacity, true, 0.85f), 1e-5f);
            Assert.AreEqual(0f, BrewCalibration.AromaMaxFor(black, 0, -1f, false, 0.85f), 1e-5f);

            float half = BrewCalibration.AromaMaxFor(black, 0, black.leafCapacity * 0.5f, false, 0.85f);
            float little = BrewCalibration.AromaMaxFor(black, 0, black.leafCapacity * 0.1f, false, 0.85f);
            Assert.That(half, Is.InRange(little, 1f));
            Assert.Greater(half, 0.5f, "saturating: half the leaves still give more than half the aroma");
        }

        [TestCase(0, 0.9f, 0.01f, InfusionCharacter.Bright)]
        [TestCase(1, 0.8f, 0.01f, InfusionCharacter.Mellow)]
        [TestCase(0, 0.9f, 0.5f, InfusionCharacter.Robust)]
        [TestCase(3, 0.1f, 0f, InfusionCharacter.Faint)]
        public void InfusionCharacterRules(int index, float aroma, float bitter, InfusionCharacter expected)
        {
            Assert.AreEqual(expected, InfusionProfile.Classify(index, aroma, bitter, 0.25f, BrewTestUtil.Quality));
        }
    }
}
