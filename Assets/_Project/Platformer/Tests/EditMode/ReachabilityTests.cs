using NUnit.Framework;
using SteepingSpirits.Platforming.Core;

namespace SteepingSpirits.Platforming.Tests
{
    public class ReachabilityTests
    {
        [Test]
        public void Reach_ShrinksWithHeightAndEndsAtPeak()
        {
            var r = new LevelReachability(LevelLayout.Parse("P.E\n###"));
            float flat = r.Reach(0);
            Assert.Greater(flat, 4f, "a running jump clears 4 tiles");
            Assert.Less(r.Reach(2), flat);
            Assert.Greater(r.Reach(-3), flat);
            Assert.AreEqual(-1f, r.Reach(5), 1e-5f);
        }

        [Test]
        public void SmallGap_IsFine_WideGap_IsNot()
        {
            var ok = new LevelReachability(LevelLayout.Parse("P.....E\n###..##"));
            CollectionAssert.IsEmpty(ok.Problems());

            var wide = new LevelReachability(LevelLayout.Parse("P..........E\n###.......##"));
            Assert.IsNotEmpty(wide.Problems());
        }

        [Test]
        public void HighWall_NeedsHelp_WindLifts()
        {
            const string wall =
                "......E\n" +
                "......#\n" +
                "......#\n" +
                "......#\n" +
                "......#\n" +
                "P.....#\n" +
                "#######\n";
            Assert.IsNotEmpty(new LevelReachability(LevelLayout.Parse(wall)).Problems());

            string withWind = wall.Replace("P.....#", "P...W.#");
            CollectionAssert.IsEmpty(new LevelReachability(LevelLayout.Parse(withWind)).Problems());
        }

        [Test]
        public void IngredientAboveHead_IsTouchableByJumping()
        {
            const string text =
                "...t..\n" +
                "......\n" +
                "......\n" +
                "P....E\n" +
                "######\n";
            CollectionAssert.IsEmpty(new LevelReachability(LevelLayout.Parse(text)).Problems());
        }

        [Test]
        public void IngredientFarAbove_IsReported()
        {
            const string text =
                "...t..\n" +
                "......\n" +
                "......\n" +
                "......\n" +
                "......\n" +
                "......\n" +
                "P....E\n" +
                "######\n";
            Assert.AreEqual(1, new LevelReachability(LevelLayout.Parse(text)).Problems().Count);
        }
    }
}
