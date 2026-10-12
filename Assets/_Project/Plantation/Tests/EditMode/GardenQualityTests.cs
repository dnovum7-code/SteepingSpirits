using NUnit.Framework;
using static SteepingSpirits.Plantation.Tests.GardenTestKit;

namespace SteepingSpirits.Plantation.Tests
{
    public class GardenQualityTests
    {
        private static HarvestQuality Grow(string plant, System.Func<int, bool> water, PlantBook book = null, GardenParams s = null)
        {
            Garden g = Make(book, s);
            Plant(g, A, plant);
            UntilRipe(g, A, water);
            return g.Apply(A, GardenAction.Harvest).harvest.quality;
        }

        [TestCase("thirsty", HarvestQuality.Excellent)]
        [TestCase("normal", HarvestQuality.Excellent)]
        [TestCase("frugal", HarvestQuality.Excellent)]
        public void DailyCare_IsExcellent_ForEveryType(string plant, HarvestQuality expected)
        {
            Assert.AreEqual(expected, Grow(plant, Always));
        }

        [Test]
        public void Thirsty_WateredEveryOtherDay_IsGood()
        {
            Assert.AreEqual(HarvestQuality.Good, Grow("thirsty", EveryOther));
        }

        [Test]
        public void Normal_WateredEveryOtherDay_IsFine()
        {
            var book = Book(Plant("n", WaterNeed.Normal, false, 3, 3));
            Assert.AreEqual(HarvestQuality.Fine, Grow("n", EveryOther, book));
        }

        [Test]
        public void Frugal_EveryOtherDay_IsAsGoodAsDaily_NeverIsGood()
        {
            Assert.AreEqual(HarvestQuality.Excellent, Grow("frugal", EveryOther));
            Assert.AreEqual(HarvestQuality.Good, Grow("frugal", Never));
        }

        [Test]
        public void FirstHarvestBonus_OnlyOnTheFirstHarvest()
        {
            var bush = Plant("tea", WaterNeed.Normal, true, 3, 3);
            bush.firstHarvestQualityBonus = 1;
            Garden g = Make(Book(bush));
            Plant(g, A, "tea");
            UntilRipe(g, A, EveryOther);
            Assert.AreEqual(HarvestQuality.Excellent, g.Apply(A, GardenAction.Harvest).harvest.quality, "fine care + first flush");
            UntilRipe(g, A, EveryOther);
            Assert.AreEqual(HarvestQuality.Fine, g.Apply(A, GardenAction.Harvest).harvest.quality);
        }

        [Test]
        public void Compost_LiftsQualityAndYield_OfTheFirstHarvestOnly()
        {
            Garden g = Make();
            g.PlotAt(A).compost = true;
            Plant(g, A, "fbush");
            UntilRipe(g, A, Never);
            HarvestResult first = g.Apply(A, GardenAction.Harvest).harvest;
            Assert.AreEqual(3, first.count);
            UntilRipe(g, A, Never);
            HarvestResult second = g.Apply(A, GardenAction.Harvest).harvest;
            Assert.AreEqual(2, second.count);
            Assert.Greater((int)first.quality, (int)second.quality);
        }

        [Test]
        public void CompostAction_MarksTheSoil_ForTheNextSowing()
        {
            Garden g = Make();
            g.Apply(A, GardenAction.Hoe);
            Assert.IsFalse(g.Apply(A, GardenAction.Compost).Done, "no compost carried");
            Assert.IsTrue(g.Apply(A, GardenAction.Compost, new ToolContext { compost = 1 }).Done);
            Assert.IsTrue(g.PlotAt(A).compost);
            g.Apply(A, GardenAction.Sow, "frugal", 1);
            Assert.IsTrue(g.PlotAt(A).crop.compost);
            Assert.IsFalse(g.PlotAt(A).compost);
        }

        [Test]
        public void Thresholds_AreTunable()
        {
            var lenient = new GardenParams { fineCare = 0.1f, excellentCare = 0.2f };
            Assert.AreEqual(HarvestQuality.Excellent, Grow("thirsty", EveryOther, null, lenient));
        }

        [Test]
        public void PerennialQuality_IsPerCycle()
        {
            Garden g = Make();
            Plant(g, A, "fbush");
            UntilRipe(g, A, Never);
            g.Apply(A, GardenAction.Harvest);
            UntilRipe(g, A, Always);
            Assert.AreEqual(HarvestQuality.Excellent, g.Apply(A, GardenAction.Harvest).harvest.quality, "a new cycle starts fresh");
        }

        [Test]
        public void ExpectedQuality_MatchesHarvest()
        {
            Garden g = Make();
            Plant(g, A, "thirsty");
            UntilRipe(g, A, EveryOther);
            HarvestQuality expected = g.ExpectedQuality(g.PlotAt(A).crop);
            Assert.AreEqual(expected, g.Apply(A, GardenAction.Harvest).harvest.quality);
        }
    }
}
