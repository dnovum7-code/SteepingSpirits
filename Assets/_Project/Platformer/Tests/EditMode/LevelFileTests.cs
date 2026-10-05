using System.IO;
using NUnit.Framework;
using SteepingSpirits.Platforming.Core;

namespace SteepingSpirits.Platforming.Tests
{
    public class LevelFileTests
    {
        [Test]
        public void AllLevelFiles_Parse()
        {
            var files = LevelFiles.All();
            Assert.IsNotEmpty(files, "no level files found");
            foreach (string f in files)
            {
                LevelLayout l = LevelLayout.Parse(File.ReadAllText(f));
                Assert.IsNotEmpty(l.Setting("id"), Path.GetFileName(f) + " needs '@id'");
                Assert.IsNotEmpty(l.Solids, Path.GetFileName(f));
            }
        }

        [Test]
        public void AllLevelFiles_HaveUniqueIds()
        {
            var ids = new System.Collections.Generic.HashSet<string>();
            foreach (string f in LevelFiles.All())
            {
                string id = LevelLayout.Parse(File.ReadAllText(f)).Setting("id");
                Assert.IsTrue(ids.Add(id), "duplicate id " + id);
            }
        }
    }
}

namespace SteepingSpirits.Platforming.Tests
{
    public class LevelDesignTests
    {
        [Test]
        public void AllLevelFiles_AreFinishableAndEverythingIsReachable()
        {
            foreach (string f in LevelFiles.All())
            {
                LevelLayout l = LevelLayout.Parse(System.IO.File.ReadAllText(f));
                var problems = new LevelReachability(l).Problems();
                Assert.IsEmpty(problems, System.IO.Path.GetFileName(f) + ":\n" + string.Join("\n", problems));
            }
        }

        [Test]
        public void Level1_TeachesTheBasics()
        {
            LevelLayout l = LevelLayout.Parse(LevelFiles.Read("Level1"));
            Assert.GreaterOrEqual(l.All(TileKind.Lantern).Count, 2, "lanterns to learn checkpoints");
            Assert.GreaterOrEqual(l.All(TileKind.Ingredient).Count, 6);
            Assert.AreEqual(0, l.All(TileKind.Bramble).Count, "nothing prickly in the first level");
            Assert.AreEqual("Level2", l.Setting("next"));

            int rare = 0;
            foreach (LevelMarker m in l.All(TileKind.Ingredient))
            {
                if (IngredientIds.IsRare(IngredientIds.FromSymbol(m.symbol))) rare++;
            }

            Assert.AreEqual(1, rare, "one optional rare find");
        }
    }
}

namespace SteepingSpirits.Platforming.Tests
{
    public class Level2DesignTests
    {
        private static bool GoalReachable(string text)
        {
            LevelLayout l = LevelLayout.Parse(text);
            var r = new LevelReachability(l);
            r.Run();
            return r.CanTouch(l.All(TileKind.Goal)[0]);
        }

        private static string Without(string text, char symbol)
        {
            // Keep header lines intact; only blank the symbol in grid rows.
            var lines = text.Replace("\r\n", "\n").Split('\n');
            for (int i = 0; i < lines.Length; i++)
            {
                if (!lines[i].StartsWith("@") && !lines[i].StartsWith("//"))
                {
                    lines[i] = lines[i].Replace(symbol, '.');
                }
            }

            return string.Join("\n", lines);
        }

        [Test]
        public void Level2_HasTwoIndependentWays()
        {
            string text = LevelFiles.Read("Level2");
            Assert.IsTrue(GoalReachable(Without(text, 'W')), "lower way (swing) alone must work");
            Assert.IsTrue(GoalReachable(Without(text, 'O')), "upper way (wind) alone must work");
            Assert.IsFalse(GoalReachable(Without(Without(text, 'O'), 'W')), "without both the gorge must block");
        }

        [Test]
        public void Level2_UsesAllSpiritElementsAndHidesRareFinds()
        {
            LevelLayout l = LevelLayout.Parse(LevelFiles.Read("Level2"));
            foreach (TileKind k in new[] { TileKind.WindSpirit, TileKind.LanternSpirit, TileKind.Swing, TileKind.DewLeaf })
            {
                Assert.IsNotEmpty(l.All(k), k.ToString());
            }

            Assert.IsNotEmpty(l.GhostPlatforms);
            Assert.IsNotEmpty(l.LeafPlatforms);

            int rare = 0;
            foreach (LevelMarker m in l.All(TileKind.Ingredient))
            {
                if (IngredientIds.IsRare(IngredientIds.FromSymbol(m.symbol))) rare++;
            }

            Assert.GreaterOrEqual(rare, 1);
        }
    }
}

namespace SteepingSpirits.Platforming.Tests
{
    public class NpcLevelTests
    {
        [Test]
        public void EverySpiritNpc_HasALineKey()
        {
            foreach (string f in LevelFiles.All())
            {
                LevelLayout l = LevelLayout.Parse(System.IO.File.ReadAllText(f));
                foreach (LevelMarker m in l.All(TileKind.Npc))
                {
                    Assert.IsNotEmpty(l.Setting("npc" + m.index), $"{System.IO.Path.GetFileName(f)}: N #{m.index} needs @npc{m.index}");
                }
            }
        }
    }
}

namespace SteepingSpirits.Platforming.Tests
{
    public class HubTests
    {
        [Test]
        public void Hub_HasReachableDoorsToEveryLevel()
        {
            LevelLayout hub = LevelLayout.Parse(LevelFiles.Read("Hub"));
            Assert.IsTrue(hub.IsHub);
            CollectionAssert.IsEmpty(new LevelReachability(hub).Problems());

            var targets = new System.Collections.Generic.HashSet<string>();
            foreach (LevelMarker d in hub.All(TileKind.Door)) targets.Add(hub.DoorTarget(d));

            foreach (string f in LevelFiles.All())
            {
                LevelLayout l = LevelLayout.Parse(System.IO.File.ReadAllText(f));
                if (!l.IsHub)
                {
                    Assert.IsTrue(targets.Contains(l.Setting("id")), "hub has no door to " + l.Setting("id"));
                }
            }

            Assert.IsTrue(targets.Contains("meadow"));
        }

        [Test]
        public void DoorWithoutTarget_IsAnError()
        {
            Assert.Throws<System.FormatException>(() => LevelLayout.Parse("@hub 1\nP.1.\n####"));
        }
    }
}

namespace SteepingSpirits.Platforming.Tests
{
    public class Level3DesignTests
    {
        private static string Without(string text, string symbols)
        {
            var lines = text.Replace("\r\n", "\n").Split('\n');
            for (int i = 0; i < lines.Length; i++)
            {
                if (lines[i].StartsWith("@") || lines[i].StartsWith("//")) continue;
                foreach (char c in symbols) lines[i] = lines[i].Replace(c, '.');
            }

            return string.Join("\n", lines);
        }

        private static bool Finishable(string text)
        {
            LevelLayout l = LevelLayout.Parse(text);
            var r = new LevelReachability(l);
            r.Run();
            return r.CanTouch(l.All(TileKind.Goal)[0]);
        }

        [Test]
        public void Level3_IsAboutSwings()
        {
            LevelLayout l = LevelLayout.Parse(LevelFiles.Read("Level3"));
            Assert.GreaterOrEqual(l.All(TileKind.Swing).Count, 5);
            Assert.AreEqual("evening", l.Setting("mood"));
            Assert.IsNotEmpty(l.All(TileKind.LanternSpirit));
            Assert.AreEqual(1, l.All(TileKind.Ingredient).FindAll(m => IngredientIds.IsRare(IngredientIds.FromSymbol(m.symbol))).Count);
        }

        [Test]
        public void Level3_GorgeHasTwoWays()
        {
            string text = LevelFiles.Read("Level3");
            Assert.IsTrue(Finishable(Without(text, "G")), "the swing chain alone must work");

            // Remove the three chain swings (x 50, 57, 63) but keep the others.
            var lines = text.Replace("\r\n", "\n").Split('\n');
            for (int i = 0; i < lines.Length; i++)
            {
                if (lines[i].StartsWith("@") || lines[i].StartsWith("//")) continue;
                char[] row = lines[i].ToCharArray();
                foreach (int x in new[] { 50, 57, 63 })
                {
                    if (x < row.Length && row[x] == 'O') row[x] = '.';
                }

                lines[i] = new string(row);
            }

            Assert.IsTrue(Finishable(string.Join("\n", lines)), "the ghost stones alone must work");
        }

        [Test]
        public void Level3_TwistNeedsSwingAndWind()
        {
            string text = LevelFiles.Read("Level3");
            Assert.IsFalse(Finishable(Without(text, "W")), "the pillar needs the wind spirit");
        }

        [Test]
        public void Bot_FinishesLevel3ViaTheSwings()
        {
            var r = RouteBotTests.Play(LevelLayout.Parse(LevelFiles.Read("Level3")));
            Assert.IsTrue(r.finished, r.log);
        }
    }
}

namespace SteepingSpirits.Platforming.Tests
{
    public class Level4DesignTests
    {
        private static bool Finishable(string text)
        {
            LevelLayout l = LevelLayout.Parse(text);
            var r = new LevelReachability(l);
            r.Run();
            return r.CanTouch(l.All(TileKind.Goal)[0]);
        }

        private static string Edit(string text, System.Func<int, int, char, char> change)
        {
            LevelLayout probe = LevelLayout.Parse(text);
            var lines = text.Replace("\r\n", "\n").Split('\n');
            int gridRow = 0;
            int firstGridLine = -1;
            for (int i = 0; i < lines.Length; i++)
            {
                if (lines[i].StartsWith("@") || lines[i].StartsWith("//")) continue;
                if (firstGridLine < 0) firstGridLine = i;
                int y = probe.Height - 1 - gridRow;
                char[] row = lines[i].ToCharArray();
                for (int x = 0; x < row.Length; x++) row[x] = change(x, y, row[x]);
                lines[i] = new string(row);
                gridRow++;
            }

            return string.Join("\n", lines);
        }

        [Test]
        public void Level4_IsAboutDewAtNight()
        {
            LevelLayout l = LevelLayout.Parse(LevelFiles.Read("Level4"));
            Assert.GreaterOrEqual(l.All(TileKind.DewLeaf).Count, 6);
            Assert.AreEqual("night", l.Setting("mood"));
            Assert.IsNotEmpty(l.All(TileKind.LanternSpirit));
            Assert.IsNotEmpty(l.GhostPlatforms);
            Assert.AreEqual(1, l.All(TileKind.Ingredient).FindAll(m => IngredientIds.IsRare(IngredientIds.FromSymbol(m.symbol))).Count);
        }

        [Test]
        public void Level4_PoolHasTwoWays()
        {
            string text = LevelFiles.Read("Level4");
            Assert.IsTrue(Finishable(Edit(text, (x, y, c) => c == 'F' ? '.' : c)), "pillar bounces alone");
            Assert.IsTrue(Finishable(Edit(text, (x, y, c) => c == 'D' && (x == 24 || x == 29 || x == 34) ? '.' : c)), "sinking leaves alone");
        }

        [Test]
        public void Level4_NightGorgeNeedsTheGhostSteps()
        {
            string text = LevelFiles.Read("Level4");
            Assert.IsFalse(Finishable(Edit(text, (x, y, c) => c == 'G' ? '.' : c)));
        }

        [Test]
        public void Bot_FinishesLevel4()
        {
            var r = RouteBotTests.Play(LevelLayout.Parse(LevelFiles.Read("Level4")));
            Assert.IsTrue(r.finished, r.log);
        }
    }
}
