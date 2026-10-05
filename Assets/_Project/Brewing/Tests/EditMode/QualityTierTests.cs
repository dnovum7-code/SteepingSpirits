using System;
using NUnit.Framework;
using SteepingSpirits.Brewing.Core;

namespace SteepingSpirits.Brewing.Tests
{
    public class QualityTierTests
    {
        [TestCase(1.0f, QualityTier.Perfect)]
        [TestCase(0.95f, QualityTier.Perfect)]
        [TestCase(0.949f, QualityTier.Harmonious)]
        [TestCase(0.80f, QualityTier.Harmonious)]
        [TestCase(0.55f, QualityTier.Decent)]
        [TestCase(0.549f, QualityTier.Flat)]
        [TestCase(0f, QualityTier.Flat)]
        public void ThresholdsMapToTiers(float harmony, QualityTier expected)
        {
            Assert.AreEqual(expected, QualityEvaluator.Classify(harmony, BrewTestUtil.Quality));
        }

        [Test]
        public void HarmonyIsClampedToZeroOne()
        {
            Assert.AreEqual(1f, QualityEvaluator.Harmony(2f, 1f), 1e-6f);
            Assert.AreEqual(0f, QualityEvaluator.Harmony(-0.5f, 1f), 1e-6f);
            Assert.AreEqual(0f, QualityEvaluator.Harmony(0.5f, 0f), 1e-6f);
        }

        [Test]
        public void CalibrationAtWindowCentreAndOptimalStopIsPerfect()
        {
            foreach (TeaParams tea in BrewTestUtil.AllTeas())
            {
                CalibrationResult cal = BrewTestUtil.Calibrate(tea);
                QualityEvaluation q = BrewTestUtil.Brew(tea, tea.IdealMid, cal.OptimalSeconds);

                Assert.AreEqual(QualityTier.Perfect, q.Tier, tea.id);
                Assert.That(cal.OptimalSeconds, Is.InRange(3f, 25f), tea.id + " optimal steep time");
            }
        }

        [Test]
        public void TeasHaveClearlyDifferentTiming()
        {
            float green = BrewTestUtil.Calibrate(TeaPresets.Green()).OptimalSeconds;
            float black = BrewTestUtil.Calibrate(TeaPresets.Black()).OptimalSeconds;
            float white = BrewTestUtil.Calibrate(TeaPresets.White()).OptimalSeconds;

            Assert.Less(green + 1f, black, "green is fastest");
            Assert.Less(black + 3f, white, "white is slowest");
        }

        [Test]
        public void GreenTeaWithBoilingWaterIsMuchWorseAndTurnsTart()
        {
            TeaParams green = TeaPresets.Green();
            float bestIdeal = BestHarmony(green, 75f);
            float bestBoiling = BestHarmony(green, 100f);

            Assert.Less(bestBoiling, bestIdeal - 0.3f);
            Assert.IsTrue(BrewTestUtil.Brew(green, 100f, 12f).IsTart, "long steep with boiling water is tart");
            Assert.IsFalse(BrewTestUtil.Brew(green, 75f, 5f).IsTart, "short ideal steep is not tart");
        }

        [Test]
        public void VeryEarlyLiftIsFlat()
        {
            foreach (TeaParams tea in BrewTestUtil.AllTeas())
            {
                Assert.AreEqual(QualityTier.Flat, BrewTestUtil.Brew(tea, tea.IdealMid, 0.5f).Tier, tea.id);
            }
        }

        [Test]
        public void StaleWaterCapsAromaButNeverFails()
        {
            TeaParams black = TeaPresets.Black();
            CalibrationResult cal = BrewTestUtil.Calibrate(black);
            QualityEvaluation fresh = BrewTestUtil.Brew(black, black.IdealMid, cal.OptimalSeconds, 1f);
            QualityEvaluation stale = BrewTestUtil.Brew(black, black.IdealMid, cal.OptimalSeconds, 0.85f);

            Assert.Less(stale.Harmony, fresh.Harmony);
            Assert.That(Enum.IsDefined(typeof(QualityTier), stale.Tier));
        }

        [Test]
        public void AnyTemperatureAndLiftTimeGivesOneOfFourTiers()
        {
            foreach (TeaParams tea in BrewTestUtil.AllTeas())
            {
                float qRef = BrewTestUtil.Calibrate(tea).QReference;
                for (float temp = 20f; temp <= 100f; temp += 10f)
                {
                    for (float lift = 0f; lift <= 90f; lift += 7.5f)
                    {
                        QualityEvaluation q = BrewCalibration.Simulate(tea, BrewTestUtil.Shape, BrewTestUtil.Quality,
                            BrewTestUtil.Room, temp, lift, 1f, qRef);
                        Assert.That(q.Harmony, Is.InRange(0f, 1f));
                        Assert.That(Enum.IsDefined(typeof(QualityTier), q.Tier));
                    }
                }
            }
        }

        [Test]
        public void PrintTuningReport()
        {
            // Not an assertion test: writes the tier windows at the window centre
            // to the test output, so tuning changes are easy to read.
            foreach (TeaParams tea in BrewTestUtil.AllTeas())
            {
                CalibrationResult cal = BrewTestUtil.Calibrate(tea);
                TestContext.WriteLine($"{tea.id}: Qref={cal.QReference:0.000} optimal={cal.OptimalSeconds:0.0}s " +
                                      $"perfect={Window(tea, QualityTier.Perfect)} " +
                                      $"harmonious+={Window(tea, QualityTier.Harmonious)} " +
                                      $"best@100°C={BestHarmony(tea, 100f):0.00}");
            }

            Assert.Pass();
        }

        private static float BestHarmony(TeaParams tea, float start)
        {
            float best = 0f;
            for (float t = 0.5f; t <= 40f; t += 0.25f)
            {
                best = Math.Max(best, BrewTestUtil.Brew(tea, start, t).Harmony);
            }

            return best;
        }

        private static string Window(TeaParams tea, QualityTier atLeast)
        {
            float first = -1f, last = -1f;
            for (float t = 0.25f; t <= 40f; t += 0.25f)
            {
                if (BrewTestUtil.Brew(tea, tea.IdealMid, t).Tier >= atLeast)
                {
                    if (first < 0f) first = t;
                    last = t;
                }
            }

            return first < 0f ? "-" : $"{first:0.0}-{last:0.0}s";
        }
    }
}
