using System;
using System.IO;
using System.Runtime.CompilerServices;
using NUnit.Framework;
using SteepingSpirits.Processing;

namespace SteepingSpirits.TeaLoop.Tests
{
    /// <summary>
    /// The brewing builder puts the brewing table on a fixed workshop cell (BrewingSandboxBuilder.TablePosition).
    /// It must stand on free floor, far enough from every station that the use key never means two things.
    /// </summary>
    public class BrewingTableTests
    {
        private const int TableX = 13, TableY = 7;

        [Test]
        public void TableCell_IsFreeFloorAwayFromStations()
        {
            WorkshopLayout layout = WorkshopLayout.Parse(File.ReadAllText(Path.Combine(RepoRoot(), "Assets/_Project/Processing/Layouts/Werkstatt.txt")));
            Assert.AreEqual(WorkshopTile.Floor, layout.At(TableX, TableY));

            // The workshop targets the station in front of the player or under them; the table is used from up to
            // 1.3 units away. Two cells of floor around it keep both apart.
            for (int dx = -2; dx <= 2; dx++)
            {
                for (int dy = -2; dy <= 2; dy++)
                {
                    WorkshopTile t = layout.At(TableX + dx, TableY + dy);
                    Assert.IsTrue(t == WorkshopTile.Floor || t == WorkshopTile.Wall, $"station {t} at {TableX + dx},{TableY + dy} next to the brewing table");
                }
            }
        }

        private static string RepoRoot([CallerFilePath] string here = "")
        {
            return Path.GetFullPath(Path.Combine(Path.GetDirectoryName(here), "..", ".."));
        }
    }
}
