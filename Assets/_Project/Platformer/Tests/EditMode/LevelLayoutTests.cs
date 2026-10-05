using System;
using NUnit.Framework;
using SteepingSpirits.Platforming.Core;

namespace SteepingSpirits.Platforming.Tests
{
    public class LevelLayoutTests
    {
        private const string Small =
            "@name Test\n" +
            "// comment line\n" +
            "..t.....E\n" +
            "P..L..===\n" +
            "####..###\n" +
            "####..###\n";

        [Test]
        public void Parse_ReadsSizeNameAndMarkers()
        {
            LevelLayout l = LevelLayout.Parse(Small);
            Assert.AreEqual("Test", l.Name);
            Assert.AreEqual(9, l.Width);
            Assert.AreEqual(4, l.Height);

            LevelMarker start = l.Start;
            Assert.AreEqual(0, start.x);
            Assert.AreEqual(2, start.y, "y counts from the bottom row");

            Assert.AreEqual(1, l.All(TileKind.Lantern).Count);
            Assert.AreEqual(1, l.All(TileKind.Ingredient).Count);
            Assert.AreEqual('t', l.All(TileKind.Ingredient)[0].symbol);
        }

        [Test]
        public void Parse_MergesSolidBlocks()
        {
            LevelLayout l = LevelLayout.Parse(Small);
            Assert.AreEqual(2, l.Solids.Count);
            CollectionAssert.Contains(l.Solids, new TileRect(0, 0, 4, 2));
            CollectionAssert.Contains(l.Solids, new TileRect(6, 0, 3, 2));
            Assert.AreEqual(1, l.OneWays.Count);
            Assert.AreEqual(new TileRect(6, 2, 3, 1), l.OneWays[0]);
        }

        [Test]
        public void Parse_SolidCoverageEqualsTileCount()
        {
            const string text =
                "P....E\n" +
                "##..##\n" +
                "######\n" +
                "#....#\n";
            LevelLayout l = LevelLayout.Parse(text);
            int area = 0;
            foreach (TileRect r in l.Solids) area += r.width * r.height;
            Assert.AreEqual(4 + 6 + 2, area);
        }

        [Test]
        public void Parse_UnknownTile_ReportsLineAndColumn()
        {
            var ex = Assert.Throws<FormatException>(() => LevelLayout.Parse("P..x.E\n######"));
            StringAssert.Contains("column 4", ex.Message);
        }

        [Test]
        public void Parse_RequiresStartAndGoal()
        {
            Assert.Throws<FormatException>(() => LevelLayout.Parse("....E\n#####"));
            Assert.Throws<FormatException>(() => LevelLayout.Parse("P....\n#####"));
            Assert.Throws<FormatException>(() => LevelLayout.Parse("P..P.E\n######"));
        }

        [Test]
        public void Parse_MarkerIndicesRunPerKind()
        {
            // A high lantern further right must count as further on than a low one on the left.
            LevelLayout l = LevelLayout.Parse("....L.E\nP.L....\n#######");
            var lanterns = l.All(TileKind.Lantern);
            Assert.AreEqual(2, lanterns.Count);
            Assert.AreEqual(2, lanterns[0].x);
            Assert.AreEqual(0, lanterns[0].index);
            Assert.AreEqual(4, lanterns[1].x);
            Assert.AreEqual(1, lanterns[1].index);
        }

        [Test]
        public void Parse_LeafPlatformsStayOneRowPerRun()
        {
            LevelLayout l = LevelLayout.Parse("P.FFF.E\n..FFF..\n#######");
            Assert.AreEqual(2, l.LeafPlatforms.Count);
            Assert.IsTrue(l.IsStandable(3, 2));
        }
    }
}
