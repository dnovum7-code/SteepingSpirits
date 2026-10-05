using NUnit.Framework;
using SteepingSpirits.Platforming.Core;

namespace SteepingSpirits.Platforming.Tests
{
    public class CheckpointTests
    {
        [Test]
        public void StartsAtStart_LanternsMoveRespawnForwardOnly()
        {
            var c = new CheckpointTracker(new Vec2(1f, 1f));
            Assert.AreEqual(new Vec2(1f, 1f), c.RespawnPoint(false));

            Assert.IsTrue(c.Light(1, new Vec2(20f, 3f)));
            Assert.AreEqual(new Vec2(20f, 3f), c.RespawnPoint(false));

            Assert.IsTrue(c.Light(0, new Vec2(10f, 2f)), "an earlier lantern can still be lit");
            Assert.AreEqual(new Vec2(20f, 3f), c.RespawnPoint(false), "but never moves the respawn back");

            Assert.IsFalse(c.Light(1, new Vec2(20f, 3f)));
            Assert.AreEqual(2, c.LitCount);
        }

        [Test]
        public void SafePoint_NeedsStableGroundAndSafeSurface()
        {
            var c = new CheckpointTracker(Vec2.Zero);
            c.TrackGround(0.1f, true, true, new Vec2(5f, 0f));
            Assert.AreEqual(Vec2.Zero, c.SafePoint, "too short");
            c.TrackGround(0.2f, true, true, new Vec2(5f, 0f));
            Assert.AreEqual(new Vec2(5f, 0f), c.SafePoint);

            c.TrackGround(1f, true, false, new Vec2(9f, 0f));
            Assert.AreEqual(new Vec2(5f, 0f), c.SafePoint, "sinking leaf is not safe");
            Assert.AreEqual(new Vec2(5f, 0f), c.RespawnPoint(true));
        }

        [Test]
        public void Catch_FadesOutMovesOnceAndFadesIn()
        {
            var p = new FeedbackParams();
            var s = new CatchSequence(p);
            Assert.IsTrue(s.Begin());
            Assert.IsFalse(s.Begin(), "no double catch");

            int moves = 0;
            float maxDark = 0f;
            float t = 0f;
            while (s.Active && t < 5f)
            {
                if (s.Tick(1f / 60f)) moves++;
                maxDark = System.Math.Max(maxDark, s.Darkness);
                t += 1f / 60f;
            }

            Assert.AreEqual(1, moves);
            Assert.AreEqual(1f, maxDark, 1e-4f);
            Assert.AreEqual(0f, s.Darkness);
            Assert.AreEqual(s.TotalSeconds, t, 0.05f);
            Assert.Less(s.TotalSeconds, 1.2f, "catch stays short");
        }
    }
}
