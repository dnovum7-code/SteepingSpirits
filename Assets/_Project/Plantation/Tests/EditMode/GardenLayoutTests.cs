using System;
using NUnit.Framework;

namespace SteepingSpirits.Plantation.Tests
{
    public class GardenLayoutTests
    {
        private const string Small =
            "@name Testbeet\n" +
            "// comment\n" +
            "#####\n" +
            "#BBW#\n" +
            "#P.L#\n" +
            "#####\n";

        [Test]
        public void Parse_ReadsGridBottomUp()
        {
            GardenLayout l = GardenLayout.Parse(Small);
            Assert.AreEqual("Testbeet", l.Name);
            Assert.AreEqual(5, l.Width);
            Assert.AreEqual(4, l.Height);
            Assert.AreEqual(GardenTile.Start, l.At(1, 1));
            Assert.AreEqual(GardenTile.Bed, l.At(1, 2));
            Assert.AreEqual(GardenTile.Well, l.At(3, 2));
            Assert.AreEqual(GardenTile.Lantern, l.At(3, 1));
            Assert.AreEqual(2, l.All(GardenTile.Bed).Count);
        }

        [Test]
        public void OutsideTheGrid_IsWall()
        {
            GardenLayout l = GardenLayout.Parse(Small);
            Assert.AreEqual(GardenTile.Wall, l.At(-1, 0));
            Assert.AreEqual(GardenTile.Wall, l.At(9, 9));
            Assert.IsFalse(l.IsWalkable(new GridPos(-1, 0)));
        }

        [Test]
        public void Walkable_BedsYes_WellAndLanternNo()
        {
            GardenLayout l = GardenLayout.Parse(Small);
            Assert.IsTrue(l.IsWalkable(new GridPos(1, 2)));
            Assert.IsTrue(l.IsWalkable(new GridPos(2, 1)));
            Assert.IsFalse(l.IsWalkable(new GridPos(3, 2)));
            Assert.IsFalse(l.IsWalkable(new GridPos(3, 1)));
            Assert.IsFalse(l.IsWalkable(new GridPos(0, 0)));
        }

        [Test]
        public void Parse_RejectsUnknownTiles_WithPosition()
        {
            var ex = Assert.Throws<FormatException>(() => GardenLayout.Parse("#P#\n#X#\n#BW\nL"));
            StringAssert.Contains("Line 2", ex.Message);
        }

        [Test]
        public void Parse_RequiresStartBedWellLantern()
        {
            Assert.Throws<FormatException>(() => GardenLayout.Parse("BWL"));
            Assert.Throws<FormatException>(() => GardenLayout.Parse("PWL"));
            Assert.Throws<FormatException>(() => GardenLayout.Parse("PBL"));
            Assert.Throws<FormatException>(() => GardenLayout.Parse("PBW"));
            Assert.Throws<FormatException>(() => GardenLayout.Parse("PPBWL"));
        }
    }
}
