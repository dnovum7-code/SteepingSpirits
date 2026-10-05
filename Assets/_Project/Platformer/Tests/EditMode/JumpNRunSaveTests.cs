using System;
using NUnit.Framework;
using SteepingSpirits.Platforming.Core;

namespace SteepingSpirits.Platforming.Tests
{
    public class JumpNRunSaveTests
    {
        [Test]
        public void FreshSave_OpensOnlyLevel1()
        {
            var s = new JumpNRunSave();
            Assert.IsTrue(s.IsUnlocked("Level1"));
            Assert.IsFalse(s.IsUnlocked("Level2"));
            Assert.AreEqual(0, s.CompletedCount);
        }

        [Test]
        public void Completing_UnlocksTheNextLevelOnce()
        {
            var s = new JumpNRunSave();
            Assert.IsTrue(s.Complete("Level1", "Level2"));
            Assert.IsFalse(s.Complete("Level1", "Level2"));
            Assert.IsTrue(s.IsUnlocked("Level2"));
            Assert.IsTrue(s.IsCompleted("Level1"));
        }

        [Test]
        public void RareFinds_ArePerSpotAndPerId()
        {
            var s = new JumpNRunSave();
            Assert.IsTrue(s.FoundRare("Level2", 61, 16, IngredientIds.StarDew));
            Assert.IsFalse(s.FoundRare("Level2", 61, 16, IngredientIds.StarDew));
            Assert.IsTrue(s.HasFoundRareAt("Level2", 61, 16));
            Assert.IsFalse(s.HasFoundRareAt("Level2", 86, 15));
            Assert.IsTrue(s.HasEverFound(IngredientIds.StarDew));
            Assert.AreEqual(1, s.RareCount);
        }

        [Test]
        public void Json_RoundTrip()
        {
            var s = new JumpNRunSave { Assists = "speed=0.8;air=1;fall=0" };
            s.Complete("Level1", "Level2");
            s.FoundRare("Level1", 54, 11, IngredientIds.GoldenTip);
            s.CompleteChallenge("Level1");
            string json = s.ToJson();
            StringAssert.StartsWith("{\"version\":1,", json);

            JumpNRunSave back = JumpNRunSave.FromJson(json);
            Assert.IsTrue(back.IsUnlocked("Level2"));
            Assert.IsTrue(back.IsCompleted("Level1"));
            Assert.IsTrue(back.HasFoundRareAt("Level1", 54, 11));
            Assert.IsTrue(back.IsChallengeDone("Level1"));
            Assert.AreEqual("speed=0.8;air=1;fall=0", back.Assists);
            Assert.AreEqual(json, back.ToJson(), "stable output");
        }

        [Test]
        public void Json_OlderFilesWithMissingFieldsStillLoad_NewerAreRejected()
        {
            JumpNRunSave s = JumpNRunSave.FromJson("{\"version\":1,\"completed\":[\"Level1\"]}");
            Assert.IsTrue(s.IsCompleted("Level1"));
            Assert.IsTrue(s.IsUnlocked("Level1"));
            Assert.Throws<FormatException>(() => JumpNRunSave.FromJson("{\"version\":2}"));
            Assert.AreEqual(0, JumpNRunSave.FromJson("  ").CompletedCount);
        }
    }
}
