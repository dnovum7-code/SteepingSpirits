using NUnit.Framework;
using static SteepingSpirits.Plantation.Tests.GardenTestKit;

namespace SteepingSpirits.Plantation.Tests
{
    public class GardenGrowthTests
    {
        [Test]
        public void Annual_RipensAfterItsStageDays_WhenCaredFor()
        {
            Garden g = Make(Book(Plant("herb", WaterNeed.Thirsty, false, 1, 2, 3)));
            Plant(g, A, "herb");
            Nights(g, A, 1, Always);
            Assert.AreEqual(1, g.PlotAt(A).crop.stage);
            Nights(g, A, 2, Always);
            Assert.AreEqual(2, g.PlotAt(A).crop.stage);
            Nights(g, A, 2, Always);
            Assert.IsFalse(g.PlotAt(A).crop.ripe);
            Nights(g, A, 1, Always);
            Assert.IsTrue(g.PlotAt(A).crop.ripe);
            Assert.AreEqual(3, g.PlotAt(A).crop.stage);
        }

        [Test]
        public void Annual_Harvest_LeavesTilledEmptySoil()
        {
            Garden g = Make();
            Plant(g, A, "normal");
            UntilRipe(g, A, Always);
            ActionOutcome o = g.Apply(A, GardenAction.Harvest);
            Assert.AreEqual(ActionEffect.Harvest, o.check.effect);
            Assert.AreEqual("normal_leaf", o.harvest.ingredientId);
            Assert.AreEqual(2, o.harvest.count);
            Assert.IsNull(g.PlotAt(A).crop);
            Assert.AreEqual(Soil.Tilled, g.PlotAt(A).soil);
        }

        [Test]
        public void Perennial_RegrowsAndRipensAgain()
        {
            Garden g = Make();
            Plant(g, A, "bush");
            Assert.AreEqual(4, UntilRipe(g, A, Always));
            g.Apply(A, GardenAction.Harvest);
            Crop c = g.PlotAt(A).crop;
            Assert.IsNotNull(c);
            Assert.IsFalse(c.ripe);
            Assert.IsTrue(c.regrowing);
            Assert.AreEqual(1, c.harvests);
            Assert.AreEqual(2, UntilRipe(g, A, Always), "regrowDays");
            g.Apply(A, GardenAction.Harvest);
            Assert.AreEqual(2, g.PlotAt(A).crop.harvests);
        }

        [Test]
        public void RipeCrop_WaitsUnchanged()
        {
            Garden g = Make();
            Plant(g, A, "normal");
            UntilRipe(g, A, Always);
            int cycleDays = g.PlotAt(A).crop.cycleDays;
            Nights(g, A, 10, Never);
            Assert.IsTrue(g.PlotAt(A).crop.ripe);
            Assert.AreEqual(cycleDays, g.PlotAt(A).crop.cycleDays, "waiting to be harvested costs no quality");
        }

        [Test]
        public void Shovel_DigsUpUnripePlant_ReturningItsSeed()
        {
            Garden g = Make();
            g.PlotAt(A).compost = true;
            Plant(g, A, "thirsty");
            Assert.IsFalse(g.PlotAt(A).compost, "compost moved into the crop");
            ActionOutcome o = g.Apply(A, GardenAction.Shovel);
            Assert.AreEqual(ActionEffect.DigUp, o.check.effect);
            Assert.AreEqual("seed_thirsty", o.seedReturned);
            Assert.IsNull(g.PlotAt(A).crop);
            Assert.AreEqual(Soil.Tilled, g.PlotAt(A).soil);
            Assert.IsTrue(g.PlotAt(A).compost, "unused compost stays in the soil");
            Assert.AreEqual(ActionEffect.Flatten, g.Apply(A, GardenAction.Shovel).check.effect);
            Assert.AreEqual(Soil.Untouched, g.PlotAt(A).soil);
        }

        [Test]
        public void SowingUnknownPlant_IsRefused()
        {
            Garden g = Make();
            g.Apply(A, GardenAction.Hoe);
            Assert.AreEqual(ActionReason.NoSeedSelected, g.Apply(A, GardenAction.Sow, "ghost_pepper", 3).check.reason);
        }

        [Test]
        public void SameSeedAndActions_GiveTheSameGarden()
        {
            string Run(int seed)
            {
                var p = Plant("rand", WaterNeed.Normal, true, 1, 1);
                p.yieldMin = 1;
                p.yieldMax = 6;
                p.seedDropChance = 0.5f;
                Garden g = Make(Book(p), new GardenParams { worldSeed = seed, rainChance = 0.3f }, new RainOracle(seed, 0.3f), 4);
                var log = new System.Text.StringBuilder();
                foreach (GridPos b in g.Beds) Plant(g, b, "rand");
                for (int day = 0; day < 40; day++)
                {
                    foreach (GridPos b in g.Beds)
                    {
                        if ((day + b.x) % 3 != 0) g.Apply(b, GardenAction.Water);
                        ActionOutcome o = g.Apply(b, GardenAction.Harvest);
                        if (o.Done) log.Append(o.harvest.count).Append(o.harvest.quality).Append(o.harvest.seeds).Append(';');
                    }

                    g.Refill();
                    log.Append(g.EndDay().rainyNext ? 'r' : '.');
                }

                return log.ToString();
            }

            Assert.AreEqual(Run(7), Run(7));
            Assert.AreNotEqual(Run(7), Run(8));
        }

        [Test]
        public void YieldsStayWithinTheirRange()
        {
            var p = Plant("rand", WaterNeed.Frugal, true, 1);
            p.yieldMin = 2;
            p.yieldMax = 4;
            p.regrowDays = 1;
            Garden g = Make(Book(p));
            Plant(g, A, "rand");
            var seen = new System.Collections.Generic.HashSet<int>();
            for (int i = 0; i < 200; i++)
            {
                g.EndDay();
                ActionOutcome o = g.Apply(A, GardenAction.Harvest);
                if (o.Done) seen.Add(o.harvest.count);
            }

            CollectionAssert.AreEquivalent(new[] { 2, 3, 4 }, seen);
        }

        [Test]
        public void SeedDrops_FollowTheirChance()
        {
            var p = Plant("seedy", WaterNeed.Frugal, true, 1);
            p.regrowDays = 1;
            p.seedDropChance = 0.25f;
            Garden g = Make(Book(p));
            Plant(g, A, "seedy");
            int harvests = 0, seeds = 0;
            for (int i = 0; i < 800; i++)
            {
                g.EndDay();
                ActionOutcome o = g.Apply(A, GardenAction.Harvest);
                if (!o.Done) continue;
                harvests++;
                seeds += o.harvest.seeds;
                Assert.AreEqual("seed_seedy", o.harvest.seedItemId);
            }

            Assert.That(seeds / (float)harvests, Is.InRange(0.18f, 0.32f));
        }
    }
}
