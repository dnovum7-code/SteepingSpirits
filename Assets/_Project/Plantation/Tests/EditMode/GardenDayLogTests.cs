using System.Collections.Generic;
using NUnit.Framework;
using SteepingSpirits.Ingredients;
using static SteepingSpirits.Plantation.Tests.GardenTestKit;

namespace SteepingSpirits.Plantation.Tests
{
    public class GardenDayLogTests
    {
        [Test]
        public void OneDay_BecomesOneParsableJsonLine()
        {
            Garden g = Make(beds: 3);
            var log = new GardenDayLog { day = g.Day, rainy = g.RainyToday };
            log.Count(g.Apply(A, GardenAction.Hoe), 10f);
            log.Count(g.Apply(A, GardenAction.Sow, "thirsty", 1), 11f);
            log.Count(g.Apply(A, GardenAction.Water), 14.5f);
            log.Count(g.Apply(B, GardenAction.Harvest), 15f); // invalid: not counted
            log.secondsInGarden = 42f;
            log.Close(g.EndDay(), g);

            string line = log.ToJsonLine();
            StringAssert.DoesNotContain("\n", line);
            Dictionary<string, object> d = MiniJson.ParseObject(line);
            Assert.AreEqual(1.0, d["day"]);
            Assert.AreEqual(1.0, d["wateredCells"]);
            Assert.AreEqual(1.0, d["growing"]);
            Assert.AreEqual(4.5, d["activeSeconds"]);
            Assert.AreEqual(42.0, d["secondsInGarden"]);
            var actions = (Dictionary<string, object>)d["actions"];
            Assert.AreEqual(1.0, actions["till"]);
            Assert.AreEqual(1.0, actions["sow"]);
            Assert.AreEqual(1.0, actions["water"]);
            Assert.IsFalse(actions.ContainsKey("harvest"));
        }

        [Test]
        public void Harvests_AreCountedByQuality()
        {
            Garden g = Make();
            Plant(g, A, "thirsty");
            UntilRipe(g, A, Always);
            var log = new GardenDayLog { day = g.Day };
            log.Count(g.Apply(A, GardenAction.Harvest), 1f);
            Assert.AreEqual(1, log.harvests);
            Assert.AreEqual(2, log.pieces);
            Assert.AreEqual(2, log.piecesByQuality[3]);
            StringAssert.Contains("\"quality\":[0,0,2]", log.ToJsonLine());
        }

        [Test]
        public void SeedChest_RestocksAnnualsWeekly()
        {
            var settings = new GardenParams { chestRestockDays = 7, chestRestockAmount = 4 };
            PlantBook book = StarterPlants.Book();
            var got = new Dictionary<string, int>();
            SeedChest.Restock(6, settings, book, got);
            Assert.AreEqual(0, got.Count);
            SeedChest.Restock(7, settings, book, got);
            Assert.AreEqual(4, got["seed_mint"]);
            Assert.AreEqual(4, got["seed_chamomile"]);
            Assert.IsFalse(got.ContainsKey("seed_tea"), "perennials need no restock");
            SeedChest.Restock(14, new GardenParams { chestRestockDays = 0 }, book, got);
            Assert.AreEqual(0, got.Count);
        }
    }
}
