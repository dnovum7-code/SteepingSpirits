using NUnit.Framework;
using static SteepingSpirits.Processing.Tests.Kit;

namespace SteepingSpirits.Processing.Tests
{
    public class TeaClassifierTests
    {
        private static readonly VarietyParams V = new VarietyParams();

        [TestCase(0.00f, false, TeaVariety.White)]
        [TestCase(0.0999f, false, TeaVariety.White)]
        [TestCase(0.00f, true, TeaVariety.Green)]
        [TestCase(0.0999f, true, TeaVariety.Green)]
        [TestCase(0.10f, false, TeaVariety.LightOolong)]
        [TestCase(0.10f, true, TeaVariety.LightOolong)]
        [TestCase(0.2999f, true, TeaVariety.LightOolong)]
        [TestCase(0.30f, true, TeaVariety.DarkOolong)]
        [TestCase(0.70f, true, TeaVariety.DarkOolong)]
        [TestCase(0.74f, true, TeaVariety.DarkOolong)]
        [TestCase(0.75f, true, TeaVariety.Black)]
        [TestCase(0.76f, true, TeaVariety.Black)]
        [TestCase(0.80f, true, TeaVariety.Black)]
        [TestCase(1.00f, true, TeaVariety.Black)]
        [TestCase(1.50f, true, TeaVariety.Black)]
        [TestCase(-0.2f, false, TeaVariety.White)]
        public void Bands_AndBoundaries(float ox, bool worked, TeaVariety expected)
        {
            Assert.AreEqual(expected, TeaClassifier.Classify(ox, worked, V));
        }

        [Test]
        public void EveryOxidation_GivesAValidTea_AndDarkerNeverGoesBack()
        {
            TeaVariety last = TeaVariety.Green;
            for (int i = 0; i <= 1000; i++)
            {
                TeaVariety t = TeaClassifier.Classify(i / 1000f, true, V);
                Assert.IsTrue(System.Enum.IsDefined(typeof(TeaVariety), t));
                Assert.GreaterOrEqual((int)t, (int)last, "monotonic at ox " + i / 1000f);
                last = t;
            }
        }

        [Test]
        public void Gaps_GoToTheNearestBand_WithTunableBands()
        {
            var wide = new VarietyParams { paleMax = 0.05f, lightOolongMin = 0.15f, lightOolongMax = 0.25f, darkOolongMin = 0.40f, darkOolongMax = 0.60f, blackMin = 0.90f };
            Assert.AreEqual(TeaVariety.Green, TeaClassifier.Classify(0.08f, true, wide));
            Assert.AreEqual(TeaVariety.LightOolong, TeaClassifier.Classify(0.12f, true, wide));
            Assert.AreEqual(TeaVariety.LightOolong, TeaClassifier.Classify(0.30f, true, wide));
            Assert.AreEqual(TeaVariety.DarkOolong, TeaClassifier.Classify(0.35f, true, wide));
            Assert.AreEqual(TeaVariety.DarkOolong, TeaClassifier.Classify(0.70f, true, wide));
            Assert.AreEqual(TeaVariety.Black, TeaClassifier.Classify(0.80f, true, wide));
        }

        [Test]
        public void ThePaths_LeadToTheirTeas()
        {
            Assert.AreEqual(TeaVariety.White, TeaClassifier.Classify(White(), V));
            Assert.AreEqual(TeaVariety.Green, TeaClassifier.Classify(Green(), V));
            Assert.AreEqual(TeaVariety.LightOolong, TeaClassifier.Classify(Oolong(0.18f), V));
            Assert.AreEqual(TeaVariety.DarkOolong, TeaClassifier.Classify(Oolong(0.5f), V));
            Assert.AreEqual(TeaVariety.Black, TeaClassifier.Classify(Black(), V));
        }

        [Test]
        public void OxidizingTooLong_GivesADarkerTea_NotAFailure()
        {
            Assert.AreEqual(TeaVariety.DarkOolong, TeaClassifier.Classify(Oolong(0.35f), V), "planned light, got dark");
            Assert.AreEqual(TeaVariety.Black, TeaClassifier.Classify(Oolong(0.85f), V), "planned dark, got black");
        }

        [Test]
        public void IngredientIds()
        {
            Assert.AreEqual("tea_white", TeaClassifier.IngredientId(TeaVariety.White));
            Assert.AreEqual("tea_oolong", TeaClassifier.IngredientId(TeaVariety.LightOolong));
            Assert.AreEqual("tea_oolong", TeaClassifier.IngredientId(TeaVariety.DarkOolong));
            Assert.AreEqual("tea_black", TeaClassifier.IngredientId(TeaVariety.Black));
        }
    }
}
