using NUnit.Framework;
using SteepingSpirits.Platforming.Core;

namespace SteepingSpirits.Platforming.Tests
{
    /// <summary>Swing combinations: swing to swing, swing into a wind column, a find reachable only in flight.</summary>
    public class SwingComboTests
    {
        public const string SwingToSwing =
            "..........O......O.............\n" +
            "...............................\n" +
            "...............................\n" +
            "...............................\n" +
            "P............................E.\n" +
            "#######..............##########\n";

        public const string SwingToWind =
            "....................E..\n" +
            "...........O.......###.\n" +
            "...................###.\n" +
            "...................###.\n" +
            "...................###.\n" +
            "P..................###.\n" +
            "#######..........W.###.\n";

        public const string FindInFlight =
            ".........................\n" +
            "...........O.T...........\n" +
            ".........................\n" +
            ".........................\n" +
            ".........................\n" +
            ".........................\n" +
            "P......................E.\n" +
            "########........#########\n";

        [Test]
        public void Reachability_KnowsTheCombinations()
        {
            CollectionAssert.IsEmpty(new LevelReachability(LevelLayout.Parse(SwingToSwing)).Problems());
            CollectionAssert.IsEmpty(new LevelReachability(LevelLayout.Parse(SwingToWind)).Problems());
            CollectionAssert.IsEmpty(new LevelReachability(LevelLayout.Parse(FindInFlight)).Problems());
        }

        [Test]
        public void WithoutTheFirstSwing_TheGapIsTooWide()
        {
            Assert.IsNotEmpty(new LevelReachability(LevelLayout.Parse(SwingToSwing.Replace("..........O......O", "..................O"))).Problems());
            Assert.IsNotEmpty(new LevelReachability(LevelLayout.Parse(SwingToWind.Replace('W', '.'))).Problems(), "the ledge needs the wind");
            Assert.IsNotEmpty(new LevelReachability(LevelLayout.Parse(FindInFlight.Replace('O', '.'))).Problems(), "the find needs the swing");
        }

        [Test]
        public void Bot_SwingsFromSwingToSwing()
        {
            var r = RouteBotTests.Play(LevelLayout.Parse(SwingToSwing), 120f);
            Assert.IsTrue(r.finished, r.log);
        }

        [Test]
        public void Bot_SwingsIntoTheWind()
        {
            var r = RouteBotTests.Play(LevelLayout.Parse(SwingToWind), 120f);
            Assert.IsTrue(r.finished, r.log);
        }
    }
}
