using NUnit.Framework;
using SteepingSpirits.Platforming.Core;

namespace SteepingSpirits.Platforming.Tests
{
    public class LanternChallengeTests
    {
        [Test]
        public void CompletesOnceWhenAllAreLit_AnyOrder()
        {
            var c = new LanternChallenge(3);
            Assert.IsFalse(c.Light(2));
            Assert.IsFalse(c.Light(2), "same lantern twice");
            Assert.IsFalse(c.Light(0));
            Assert.IsFalse(c.Complete);
            Assert.IsTrue(c.Light(1));
            Assert.IsTrue(c.Complete);
            Assert.IsFalse(c.Light(1));
            Assert.AreEqual(IngredientIds.SpringCrystal, c.RewardId);
        }

        [Test]
        public void NoPathLanterns_MeansNoChallenge()
        {
            var c = new LanternChallenge(0);
            Assert.IsFalse(c.Active);
            Assert.IsFalse(c.Complete);
            Assert.IsFalse(c.Light(0));
        }

        [Test]
        public void PathLanterns_ParseAndCountForReachability()
        {
            LevelLayout l = LevelLayout.Parse("P.l..E\n######");
            Assert.AreEqual(1, l.All(TileKind.PathLantern).Count);
            Assert.AreEqual(0, l.All(TileKind.Lantern).Count, "not a checkpoint");

            const string high =
                "..l...\n" +
                "......\n" +
                "......\n" +
                "......\n" +
                "......\n" +
                "......\n" +
                "P....E\n" +
                "######\n";
            Assert.IsNotEmpty(new LevelReachability(LevelLayout.Parse(high)).Problems());
        }

        [Test]
        public void Levels_HaveAPathLanternChallenge()
        {
            foreach (string f in LevelFiles.All())
            {
                LevelLayout l = LevelLayout.Parse(System.IO.File.ReadAllText(f));
                if (l.IsHub) continue;
                Assert.GreaterOrEqual(l.All(TileKind.PathLantern).Count, 3, System.IO.Path.GetFileName(f));
            }
        }
    }
}
