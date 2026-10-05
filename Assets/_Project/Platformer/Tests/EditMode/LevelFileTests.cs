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
