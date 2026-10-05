using System.IO;
using NUnit.Framework;
using SteepingSpirits.Platforming.Core;

namespace SteepingSpirits.Platforming.Tests
{
    public class LevelLintTests
    {
        [Test]
        public void ShippedLevels_HaveNoWarnings()
        {
            foreach (string f in LevelFiles.All())
            {
                var w = LevelLint.Check(LevelLayout.Parse(File.ReadAllText(f)));
                Assert.IsEmpty(w, Path.GetFileName(f) + ":\n" + string.Join("\n", w));
            }
        }

        [Test]
        public void ReportsUnreachableIngredient()
        {
            const string text =
                "...t..\n" + "......\n" + "......\n" + "......\n" + "......\n" + "......\n" +
                "P....E\n" + "######\n";
            Assert.IsTrue(LevelLint.Check(LevelLayout.Parse(text)).Exists(w => w.Contains("Ingredient")));
        }

        [Test]
        public void ReportsLanternsOutOfOrder()
        {
            // The way goes right along the bottom, then back left on the upper floor:
            // the left upper lantern (#0 by x) is reached after the right one (#1).
            const string text =
                "........................\n" +
                "E...L...................\n" +
                "###############.........\n" +
                "................##......\n" +
                "...................##...\n" +
                "P.................L.....\n" +
                "########################\n";
            var w = LevelLint.Check(LevelLayout.Parse(text));
            Assert.IsTrue(w.Exists(x => x.Contains("lantern order")), string.Join("\n", w));
        }

        [Test]
        public void ReportsSwingRopeThroughGround()
        {
            const string text =
                "....O....\n" +
                "....#....\n" +
                ".........\n" +
                "P.......E\n" +
                "#########\n";
            Assert.IsTrue(LevelLint.Check(LevelLayout.Parse(text)).Exists(w => w.Contains("rope")));
        }

        [Test]
        public void ReportsGhostPlatformsWithoutLanternSpirit()
        {
            const string text =
                "P....E\n" +
                "##GG##\n";
            Assert.IsTrue(LevelLint.Check(LevelLayout.Parse(text)).Exists(w => w.Contains("lantern spirit")));
            Assert.IsFalse(LevelLint.Check(LevelLayout.Parse("P.S..E\n##GG##\n")).Exists(w => w.Contains("lantern spirit")));
        }
    }
}
