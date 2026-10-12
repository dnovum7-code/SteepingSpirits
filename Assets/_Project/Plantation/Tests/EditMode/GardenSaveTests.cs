using System;
using NUnit.Framework;
using static SteepingSpirits.Plantation.Tests.GardenTestKit;

namespace SteepingSpirits.Plantation.Tests
{
    public class GardenSaveTests
    {
        private static string Snapshot(Garden g)
        {
            var sb = new System.Text.StringBuilder().Append(g.Day).Append('/').Append(g.CanWater).Append('/').Append(g.RainyToday);
            foreach (GridPos p in g.Beds)
            {
                Plot x = g.PlotAt(p);
                sb.Append('|').Append(p).Append(x.soil).Append(x.watered).Append(x.idleDays).Append(x.compost);
                if (x.crop != null)
                {
                    Crop c = x.crop;
                    sb.Append(c.plantId).Append(c.stage).Append(c.progress).Append(c.ripe).Append(c.harvests).Append(c.regrowing)
                        .Append(c.forgivenUsed).Append(c.cycleDays).Append(c.cyclePoints).Append(c.wateredYesterday).Append(c.waterHistory).Append(c.compost);
                }
            }

            return sb.ToString();
        }

        private static Garden Busy()
        {
            Garden g = Make(beds: 5, weather: new FixedWeather(3));
            g.PlotAt(new GridPos(4, 0)).compost = true;
            Plant(g, new GridPos(0, 0), "thirsty");
            Plant(g, new GridPos(1, 0), "bush");
            Plant(g, new GridPos(4, 0), "normal");
            g.Apply(new GridPos(2, 0), GardenAction.Hoe);
            for (int i = 0; i < 6; i++)
            {
                g.Apply(new GridPos(0, 0), GardenAction.Water);
                if (i % 2 == 0) g.Apply(new GridPos(1, 0), GardenAction.Water);
                g.Apply(new GridPos(1, 0), GardenAction.Harvest);
                g.EndDay();
            }

            g.Apply(new GridPos(0, 0), GardenAction.Water);
            return g;
        }

        [Test]
        public void RoundTrip_RestoresEveryCell()
        {
            Garden g = Busy();
            GardenSave save = GardenSave.From(g);
            save.chestOpened = true;
            save.chestSeeds["seed_mint"] = 3;
            save.seedBackup["seed_tea"] = 2;
            save.wideCan = true;
            save.gardenHelp = false;
            save.clockDay = 17;
            string json = save.ToJson();

            GardenSave back = GardenSave.FromJson(json);
            Garden h = Make(beds: 5, weather: new FixedWeather(3));
            back.ApplyTo(h);
            Assert.AreEqual(Snapshot(g), Snapshot(h));
            Assert.IsTrue(back.chestOpened);
            Assert.AreEqual(3, back.chestSeeds["seed_mint"]);
            Assert.AreEqual(2, back.seedBackup["seed_tea"]);
            Assert.IsTrue(back.wideCan);
            Assert.IsFalse(back.gardenHelp);
            Assert.AreEqual(17, back.clockDay);
            Assert.AreEqual(json, back.ToJson(), "stable output");
        }

        [Test]
        public void RestoredGarden_ContinuesExactlyLikeTheOriginal()
        {
            Garden g = Busy();
            Garden h = Make(beds: 5, weather: new FixedWeather(3));
            GardenSave.FromJson(GardenSave.From(g).ToJson()).ApplyTo(h);
            for (int i = 0; i < 10; i++)
            {
                foreach (GridPos p in g.Beds)
                {
                    Assert.AreEqual(g.Apply(p, GardenAction.Harvest).harvest.count, h.Apply(p, GardenAction.Harvest).harvest.count);
                }

                g.EndDay();
                h.EndDay();
            }

            Assert.AreEqual(Snapshot(g), Snapshot(h));
        }

        [Test]
        public void ChangedLayout_KeepsMatchingCells_DropsOthers()
        {
            Garden g = Busy();
            Garden smaller = Make(beds: 2, weather: new FixedWeather(3));
            GardenSave.From(g).ApplyTo(smaller);
            Assert.AreEqual("thirsty", smaller.PlotAt(new GridPos(0, 0)).crop.plantId);
            Assert.IsNull(smaller.PlotAt(new GridPos(4, 0)));
        }

        [Test]
        public void UnknownPlants_AreDropped_SoilKept()
        {
            string json = "{\"version\":1,\"day\":4,\"can\":5,\"plots\":[{\"x\":0,\"y\":0,\"soil\":1,\"crop\":{\"plant\":\"ghost_pepper\",\"stage\":1}}]}";
            Garden g = Make();
            GardenSave.FromJson(json).ApplyTo(g);
            Assert.AreEqual(Soil.Tilled, g.PlotAt(A).soil);
            Assert.IsNull(g.PlotAt(A).crop);
            Assert.AreEqual(4, g.Day);
            Assert.AreEqual(5, g.CanWater);
        }

        [Test]
        public void EmptyText_IsAFreshSave_NewerVersionIsRefused()
        {
            Assert.AreEqual(0, GardenSave.FromJson("").plots.Count);
            Assert.IsTrue(GardenSave.FromJson(" ").gardenHelp);
            Assert.Throws<FormatException>(() => GardenSave.FromJson("{\"version\":99}"));
            Assert.Throws<FormatException>(() => GardenSave.FromJson("[1,2]"));
        }

        [Test]
        public void CanIsClampedToCapacity()
        {
            Garden g = Make(settings: new GardenParams { canCapacity = 5 });
            GardenSave.FromJson("{\"version\":1,\"day\":2,\"can\":40}").ApplyTo(g);
            Assert.AreEqual(5, g.CanWater);
        }
    }
}
