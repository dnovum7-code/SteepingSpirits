using System.IO;
using NUnit.Framework;

namespace SteepingSpirits.Plantation.Tests
{
    /// <summary>Plays the real garden layout for 28 days with three bot styles.</summary>
    public class GardenSimulatorTests
    {
        private const int Days = 28;

        /// <summary>Finds the layout from the Unity project root or from a dotnet bin folder.</summary>
        public static GardenLayout RealLayout()
        {
            const string rel = "Assets/_Project/Plantation/Layouts/Teegarten.txt";
            string dir = Directory.GetCurrentDirectory();
            for (int i = 0; i < 8 && dir != null; i++)
            {
                string path = Path.Combine(dir, rel);
                if (File.Exists(path)) return GardenLayout.Parse(File.ReadAllText(path));
                dir = Path.GetDirectoryName(dir);
            }

            Assert.Fail("Layout not found: " + rel);
            return null;
        }

        private static SimResult Run(BotStyle style, bool wideCan = false)
        {
            SimResult r = GardenSimulator.Run(RealLayout(), new GardenParams(), StarterPlants.Book(), style, Days, null, wideCan);
            TestContext.WriteLine(r.ToString());
            return r;
        }

        [Test]
        public void CarefulHarvestsMostAndBest_SloppyStillHarvests_NothingDies()
        {
            SimResult careful = Run(BotStyle.Careful);
            SimResult sloppy = Run(BotStyle.Sloppy);
            SimResult never = Run(BotStyle.NeverWater);

            Assert.Greater(careful.pieces, sloppy.pieces);
            Assert.Greater(sloppy.pieces, never.pieces);
            Assert.Greater(careful.QualityScore, sloppy.QualityScore);
            Assert.GreaterOrEqual(sloppy.QualityScore, never.QualityScore);
            Assert.Greater(sloppy.pieces, careful.pieces / 3, "sloppy play still harvests well");
            Assert.Greater(never.pieces, 0, "the frugal plant gives something even without water");
            foreach (SimResult r in new[] { careful, sloppy, never })
            {
                Assert.AreEqual(0, r.plantsLost, r.style + ": no plant may die");
            }
        }

        [Test]
        public void EveryPlantGetsHarvested_WhenCaredFor()
        {
            SimResult careful = Run(BotStyle.Careful);
            foreach (string id in StarterPlants.Ids)
            {
                Assert.IsTrue(careful.piecesByPlant.ContainsKey(id), id + " never ripened in " + Days + " days");
            }
        }

        [Test]
        public void DailyRound_TakesOnlyAFewMinutes()
        {
            SimResult careful = Run(BotStyle.Careful);
            SimResult wide = Run(BotStyle.Careful, true);
            // A bot walks perfectly; a person needs maybe twice as long. Few minutes = under 3 for a person.
            Assert.Less(careful.AverageRound, 90f, "average round");
            Assert.Less(careful.MaxRound, 120f, "longest round (planting day)");
            Assert.LessOrEqual(wide.AverageRound, careful.AverageRound, "the 1x3 can never makes it slower");
        }

        [Test]
        public void Simulation_IsDeterministic()
        {
            Assert.AreEqual(Run(BotStyle.Sloppy).ToString(), Run(BotStyle.Sloppy).ToString());
        }

        [Test]
        public void LayoutBlocks_MatchTheStarterPack()
        {
            GardenLayout l = RealLayout();
            Assert.AreEqual(6, l.BedBlocks().Count);
            Assert.AreEqual(6, l.SeedPack().Count);
            foreach (var kv in l.SeedPack()) Assert.IsNotNull(StarterPlants.Create(kv.Key), kv.Key);
        }
    }
}
