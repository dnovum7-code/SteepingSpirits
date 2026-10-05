using NUnit.Framework;
using SteepingSpirits.Platforming.Core;

namespace SteepingSpirits.Platforming.Tests
{
    public class SectionTelemetryTests
    {
        [Test]
        public void SplitsAtNewLanternsOnly()
        {
            var t = new SectionTelemetry("run1", "Level1", 0f, "speed=1.0;air=0;fall=0");
            t.Fall();
            t.Ingredient(IngredientIds.TeaLeaf);
            t.Ingredient(IngredientIds.TeaLeaf);
            SectionRecord first = t.LanternLit(0, 12.5f);
            Assert.IsNotNull(first);
            Assert.AreEqual(0, first.section);
            Assert.AreEqual(12.5f, first.seconds, 1e-4f);
            Assert.AreEqual(1, first.falls);
            Assert.AreEqual(2, first.ingredients[IngredientIds.TeaLeaf]);
            Assert.AreEqual("lantern", first.end);

            Assert.IsNull(t.LanternLit(0, 13f), "same lantern again");
            t.Fall();
            SectionRecord second = t.LanternLit(2, 20f);
            Assert.AreEqual(1, second.section);
            Assert.AreEqual(7.5f, second.seconds, 1e-4f);
            Assert.AreEqual(1, second.falls);
            Assert.IsNull(t.LanternLit(1, 21f), "an earlier lantern never opens a section");

            SectionRecord last = t.Goal(30f);
            Assert.AreEqual(2, last.section);
            Assert.AreEqual("goal", last.end);
            Assert.IsNull(t.Left(31f), "nothing open after the goal");
            Assert.AreEqual(3, t.Finished.Count);
        }

        [Test]
        public void LeavingMidSection_IsRecorded()
        {
            var t = new SectionTelemetry("r", "L", 5f, "");
            SectionRecord r = t.Left(8f);
            Assert.AreEqual("left", r.end);
            Assert.AreEqual(3f, r.seconds, 1e-4f);
        }

        [Test]
        public void Json_IsOneLineAndEscaped()
        {
            var t = new SectionTelemetry("r\"1", "Level1", 0f, "speed=0.8;air=1;fall=0");
            t.Ingredient(IngredientIds.Herb);
            t.Ingredient(IngredientIds.Blossom);
            SectionRecord r = t.Goal(3.25f);
            string json = SectionTelemetry.ToJson(r, "2026-10-05T10:00:00Z");
            Assert.AreEqual(
                "{\"time\":\"2026-10-05T10:00:00Z\",\"run\":\"r\\\"1\",\"level\":\"Level1\",\"section\":0,\"seconds\":3.25," +
                "\"falls\":0,\"ingredients\":{\"blossom\":1,\"herb\":1},\"assists\":\"speed=0.8;air=1;fall=0\",\"end\":\"goal\"}",
                json);
            StringAssert.DoesNotContain("\n", json);
        }
    }
}
