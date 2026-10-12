using NUnit.Framework;
using static SteepingSpirits.Plantation.Tests.GardenTestKit;

namespace SteepingSpirits.Plantation.Tests
{
    public class GardenDayTests
    {
        [Test]
        public void WateredSoil_DriesOverNight()
        {
            Garden g = Make();
            g.Apply(A, GardenAction.Hoe);
            Assert.IsTrue(g.Apply(A, GardenAction.Water).Done);
            Assert.IsTrue(g.PlotAt(A).watered);
            DayReport r = g.EndDay();
            Assert.AreEqual(1, r.wateredCells);
            Assert.IsFalse(g.PlotAt(A).watered);
            Assert.AreEqual(2, g.Day);
        }

        [Test]
        public void Rain_WatersTilledCells_InTheMorning_ButNotUntouchedOnes()
        {
            Garden g = Make(weather: new FixedWeather(2));
            g.Apply(A, GardenAction.Hoe);
            DayReport r = g.EndDay();
            Assert.IsTrue(r.rainyNext);
            Assert.IsTrue(g.RainyToday);
            Assert.IsTrue(g.PlotAt(A).watered);
            Assert.IsFalse(g.PlotAt(B).watered);
        }

        [Test]
        public void TillingOnARainyDay_GivesWetSoil()
        {
            Garden g = Make(weather: new FixedWeather(1));
            g.Apply(A, GardenAction.Hoe);
            Assert.IsTrue(g.PlotAt(A).watered);
            Assert.AreEqual(ActionReason.AlreadyWatered, g.Check(A, GardenAction.Water).reason);
        }

        [Test]
        public void TilledEmptySoil_TurnsBackToUntouched_AfterDecayDays()
        {
            var s = new GardenParams { tilledDecayDays = 3 };
            Garden g = Make(settings: s);
            g.Apply(A, GardenAction.Hoe);
            g.EndDay();
            g.EndDay();
            Assert.AreEqual(Soil.Tilled, g.PlotAt(A).soil);
            DayReport r = g.EndDay();
            Assert.AreEqual(Soil.Untouched, g.PlotAt(A).soil);
            CollectionAssert.Contains(r.reverted, A);
        }

        [Test]
        public void SownSoil_NeverReverts()
        {
            Garden g = Make();
            Plant(g, A, "thirsty");
            for (int i = 0; i < 20; i++) g.EndDay();
            Assert.AreEqual(Soil.Tilled, g.PlotAt(A).soil);
            Assert.IsNotNull(g.PlotAt(A).crop);
        }

        [Test]
        public void CatchUp_ProcessesMissedNights_AsDryNights()
        {
            Garden g = Make();
            Plant(g, A, "thirsty");
            g.Apply(A, GardenAction.Water);
            var reports = g.CatchUpTo(6);
            Assert.AreEqual(5, reports.Count);
            Assert.AreEqual(6, g.Day);
            Assert.AreEqual(1, g.PlotAt(A).crop.progress, "only the watered first night counted");
            Assert.AreEqual(0, g.CatchUpTo(3).Count, "never goes back");
        }

        [Test]
        public void CatchUp_LongAbsence_IsCapped()
        {
            Garden g = Make(settings: new GardenParams { maxCatchUpDays = 10 });
            Assert.AreEqual(10, g.CatchUpTo(1001).Count);
            Assert.AreEqual(1001, g.Day);
        }

        [Test]
        public void Can_EmptiesAndRefills()
        {
            Garden g = Make(settings: new GardenParams { canCapacity = 1 });
            g.Apply(A, GardenAction.Hoe);
            g.Apply(B, GardenAction.Hoe);
            Assert.IsTrue(g.Apply(A, GardenAction.Water).Done);
            Assert.AreEqual(0, g.CanWater);
            Assert.AreEqual(ActionReason.CanEmpty, g.Apply(B, GardenAction.Water).check.reason);
            Assert.AreEqual(1, g.Refill());
            Assert.IsTrue(g.Apply(B, GardenAction.Water).Done);
        }

        [Test]
        public void RainOracle_IsDeterministic_AndNearItsChance()
        {
            var a = new RainOracle(42, 0.15f);
            var b = new RainOracle(42, 0.15f);
            int rainy = 0;
            for (int d = 1; d <= 2000; d++)
            {
                Assert.AreEqual(a.IsRainy(d), b.IsRainy(d));
                if (a.IsRainy(d)) rainy++;
            }

            Assert.That(rainy / 2000f, Is.InRange(0.12f, 0.18f));
            Assert.IsFalse(new RainOracle(42, 0f).IsRainy(5));
        }

        [Test]
        public void ManualClock_CountsUp()
        {
            IDayClock c = new ManualDayClock(3);
            c.EndDay();
            Assert.AreEqual(4, c.Today);
        }
    }
}
