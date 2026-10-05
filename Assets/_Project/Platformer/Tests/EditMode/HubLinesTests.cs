using NUnit.Framework;
using SteepingSpirits.Platforming.Core;

namespace SteepingSpirits.Platforming.Tests
{
    public class HubLinesTests
    {
        [Test]
        public void TripSpirit_FollowsProgress()
        {
            var s = new JumpNRunSave();
            Assert.AreEqual("hub_welcome", HubLines.Pick("hub", s, 3, 5));
            s.Complete("Level1", "Level2");
            Assert.AreEqual("hub_first", HubLines.Pick("hub", s, 3, 5));
            s.Complete("Level2", "Level3");
            Assert.AreEqual("hub_more", HubLines.Pick("hub", s, 3, 5));
            s.Complete("Level3", "");
            Assert.AreEqual("hub_all", HubLines.Pick("hub", s, 3, 5));
            s.CompleteChallenge("Level1");
            s.CompleteChallenge("Level2");
            s.CompleteChallenge("Level3");
            Assert.AreEqual("hub_all_lanterns", HubLines.Pick("hub", s, 3, 5));
        }

        [Test]
        public void ShelfSpirit_CountsRareFinds()
        {
            var s = new JumpNRunSave();
            Assert.AreEqual("shelf_empty", HubLines.Pick("hub_shelf", s, 3, 5));
            s.FoundRare("Level1", 1, 1, IngredientIds.GoldenTip);
            Assert.AreEqual("shelf_some", HubLines.Pick("hub_shelf", s, 3, 5));
            s.FoundRare("Level2", 1, 1, IngredientIds.StarDew);
            s.FoundRare("Level2", 2, 2, IngredientIds.SpiritBlossom);
            Assert.AreEqual("shelf_half", HubLines.Pick("hub_shelf", s, 3, 5));
            s.FoundRare("Level3", 1, 1, IngredientIds.MoonHerb);
            s.FoundRare("Level1", 9, 9, IngredientIds.SpringCrystal);
            Assert.AreEqual("shelf_full", HubLines.Pick("hub_shelf", s, 3, 5));
        }

        [Test]
        public void OtherKeys_PassThrough()
        {
            Assert.AreEqual("grove_wind", HubLines.Pick("grove_wind", new JumpNRunSave(), 3, 5));
        }
    }
}
