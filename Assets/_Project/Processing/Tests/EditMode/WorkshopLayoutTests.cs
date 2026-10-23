using System;
using System.IO;
using NUnit.Framework;

namespace SteepingSpirits.Processing.Tests
{
    public class WorkshopLayoutTests
    {
        public static WorkshopLayout Real()
        {
            const string rel = "Assets/_Project/Processing/Layouts/Werkstatt.txt";
            string dir = Directory.GetCurrentDirectory();
            for (int i = 0; i < 8 && dir != null; i++)
            {
                string path = Path.Combine(dir, rel);
                if (File.Exists(path)) return WorkshopLayout.Parse(File.ReadAllText(path));
                dir = Path.GetDirectoryName(dir);
            }

            Assert.Fail("Layout not found: " + rel);
            return null;
        }

        [Test]
        public void RealLayout_HasEveryStation_AndAStarterPack()
        {
            WorkshopLayout l = Real();
            Assert.AreEqual("Teewerkstatt", l.Name);
            Assert.AreEqual(1, l.All(WorkshopTile.Door).Count);
            Assert.AreEqual(2, l.All(WorkshopTile.Rack).Count);
            Assert.AreEqual(4, l.StarterPack().Count);
            Assert.AreEqual(("tea_leaf_first", 4, 3), l.StarterPack()[3]);
        }

        [Test]
        public void EveryStation_CanBeReachedOnFoot()
        {
            WorkshopLayout l = Real();
            var seen = new bool[l.Width, l.Height];
            var open = new System.Collections.Generic.Queue<(int, int)>();
            (int sx, int sy) = l.All(WorkshopTile.Start)[0];
            open.Enqueue((sx, sy));
            seen[sx, sy] = true;
            while (open.Count > 0)
            {
                (int x, int y) = open.Dequeue();
                foreach ((int dx, int dy) in new[] { (1, 0), (-1, 0), (0, 1), (0, -1) })
                {
                    int nx = x + dx, ny = y + dy;
                    if (!l.IsWalkable(nx, ny) || seen[nx, ny]) continue;
                    seen[nx, ny] = true;
                    open.Enqueue((nx, ny));
                }
            }

            for (int y = 0; y < l.Height; y++)
            for (int x = 0; x < l.Width; x++)
            {
                WorkshopTile t = l.At(x, y);
                if (t == WorkshopTile.Wall || t == WorkshopTile.Floor || t == WorkshopTile.Start || t == WorkshopTile.Door) continue;
                bool next = (l.IsWalkable(x + 1, y) && seen[x + 1, y]) || (l.IsWalkable(x - 1, y) && seen[x - 1, y]) ||
                            (l.IsWalkable(x, y + 1) && seen[x, y + 1]) || (l.IsWalkable(x, y - 1) && seen[x, y - 1]);
                Assert.IsTrue(next, t + " at " + x + "," + y + " cannot be reached");
            }
        }

        [Test]
        public void BrokenLayouts_SayWhy()
        {
            StringAssert.Contains("Line 1", Assert.Throws<FormatException>(() => WorkshopLayout.Parse("#X#")).Message);
            Assert.Throws<FormatException>(() => WorkshopLayout.Parse("SVRTOKD"));
            Assert.Throws<FormatException>(() => WorkshopLayout.Parse("PSVRTOK"));
        }
    }
}
