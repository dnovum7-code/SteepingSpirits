using NUnit.Framework;
using static SteepingSpirits.Plantation.Tests.GardenTestKit;

namespace SteepingSpirits.Plantation.Tests
{
    public class GardenWaterTests
    {
        [Test]
        public void Thirsty_GrowsOnlyOnWateredDays()
        {
            Garden g = Make();
            Plant(g, A, "thirsty");
            Nights(g, A, 3, Never);
            Assert.AreEqual(0, g.PlotAt(A).crop.progress);
            Assert.AreEqual(0, g.PlotAt(A).crop.stage);
            Assert.AreEqual(8, UntilRipe(g, A, EveryOther), "4 growth days need 8 days when watered every other day");
        }

        [Test]
        public void Normal_ForgivesOneDryDayPerStage()
        {
            Garden g = Make(Book(Plant("n", WaterNeed.Normal, false, 3, 3)));
            Plant(g, A, "n");
            Nights(g, A, 1, Never);
            Assert.AreEqual(1, g.PlotAt(A).crop.progress, "first dry day forgiven");
            Nights(g, A, 1, Never);
            Assert.AreEqual(1, g.PlotAt(A).crop.progress, "second dry day in the same stage waits");
            Nights(g, A, 2, Always);
            Assert.AreEqual(1, g.PlotAt(A).crop.stage);
            Nights(g, A, 1, Never);
            Assert.AreEqual(1, g.PlotAt(A).crop.progress, "the next stage forgives again");
        }

        [Test]
        public void Normal_NeverWatered_StillGrowsOneDayPerStage()
        {
            Garden g = Make(Book(Plant("n", WaterNeed.Normal, false, 1, 1)));
            Plant(g, A, "n");
            Assert.AreEqual(2, UntilRipe(g, A, Never), "one-day stages ripen on forgiveness alone");
        }

        [Test]
        public void Frugal_GrowsWithoutWater()
        {
            Garden g = Make();
            Plant(g, A, "frugal");
            Assert.AreEqual(4, UntilRipe(g, A, Never));
        }

        [Test]
        public void NoPlantEverDies_FromNeglect()
        {
            Garden g = Make(beds: 4);
            Plant(g, new GridPos(0, 0), "thirsty");
            Plant(g, new GridPos(1, 0), "normal");
            Plant(g, new GridPos(2, 0), "frugal");
            Plant(g, new GridPos(3, 0), "bush");
            for (int i = 0; i < 100; i++) g.EndDay();
            foreach (GridPos p in g.Beds)
            {
                Assert.IsNotNull(g.PlotAt(p).crop, p.ToString());
                Assert.AreEqual(Soil.Tilled, g.PlotAt(p).soil);
            }
        }

        [Test]
        public void Rain_CountsAsWatering()
        {
            var days = new int[30];
            for (int i = 0; i < days.Length; i++) days[i] = i + 1;
            Garden g = Make(weather: new FixedWeather(days));
            Plant(g, A, "thirsty");
            Assert.AreEqual(4, UntilRipe(g, A, Never));
            Assert.AreEqual(HarvestQuality.Excellent, g.Apply(A, GardenAction.Harvest).harvest.quality);
        }

        [Test]
        public void WaterHistory_RecordsLastDays()
        {
            Garden g = Make();
            Plant(g, A, "frugal");
            Nights(g, A, 3, d => d != 2);
            Assert.AreEqual(0b101, g.PlotAt(A).crop.waterHistory);
        }

        [Test]
        public void GardenHelp_SaysWhoWantsWater()
        {
            Garden g = Make(beds: 3);
            var t = new GridPos(0, 0);
            var f = new GridPos(1, 0);
            var empty = new GridPos(2, 0);
            Plant(g, t, "thirsty");
            Plant(g, f, "frugal");
            g.Apply(empty, GardenAction.Hoe);
            Assert.IsTrue(g.WantsWater(t));
            Assert.IsTrue(g.WantsWater(f));
            Assert.IsFalse(g.WantsWater(empty));
            g.Apply(f, GardenAction.Water);
            Assert.IsFalse(g.WantsWater(f), "already watered");
            g.EndDay();
            Assert.IsFalse(g.WantsWater(f), "frugal watered yesterday is fine today");
            Assert.IsTrue(g.WantsWater(t));
        }
    }
}
